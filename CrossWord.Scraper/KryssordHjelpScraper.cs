using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace CrossWord.Scraper
{
    public class KryssordHjelpScraper
    {
        TextWriter writer = null;
        string connectionString = null;
        string signalRHubURL = null;

        public KryssordHjelpScraper(string connectionString, string signalRHubURL, int letterCount)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(signalRHubURL, letterCount.ToString());
            writer.WriteLine("Starting KryssordHjelp Scraper ....");

            // make sure that no chrome and chrome drivers are running
            // KillAllChromeDriverInstances();

            DoScrape(letterCount);
        }

        private void DoScrape(int letterCount)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
                Word lastWord = GetLastWordFromLetterCount(db, letterCount);
                string lastWordString = lastWord != null ? lastWord.Value : null;

                using (var driver = ChromeDriverUtils.GetChromeDriver())
                {
                    // get first user
                    var user = db.DictionaryUsers.OrderBy(u => u.UserId).FirstOrDefault();

                    // read all words with the letter count
                    ReadWordsByWordPermutations(letterCount, driver, db, user, lastWordString);
                }
            }
        }

        private Word GetLastWordFromLetterCount(WordHintDbContext db, int letterCount)
        {
            if (letterCount > 0)
            {
                Log.Information("Looking for last word using letter count '{0}'", letterCount);

                var lastWordWithPatternLength = db.Words.Where(w => w.NumberOfLetters == letterCount).OrderByDescending(p => p.WordId).FirstOrDefault();
                if (lastWordWithPatternLength != null)
                {
                    Log.Information("Using the last word with letter count '{0}', last word '{1}'", letterCount, lastWordWithPatternLength);
                    return lastWordWithPatternLength;
                }
            }

            return null;
        }

        private void ReadWordsByWordPermutations(int letterCount, IWebDriver driver, WordHintDbContext db, User user, string lastWord)
        {
            int permutationSize = 1;

            var alphabet = "abcdefghijklmnopqrstuvwxyzøæå";
            var permutations = alphabet.Select(x => x.ToString());

            for (int i = 0; i < permutationSize - 1; i++)
            {
                permutations = permutations.SelectMany(x => alphabet, (x, y) => x + y);
            }

            bool hasFoundWord = false;
            foreach (var permutation in permutations)
            {
                string wordPattern = "";
                if (letterCount > permutationSize)
                {
                    // make word search pattern                    
                    wordPattern = permutation.PadRight(letterCount, '?');
                }
                else
                {
                    wordPattern = permutation;
                }

                // if lastWord isn't null 
                if (lastWord != null)
                {
                    // skip until lastWord is found
                    if (!hasFoundWord)
                    {
                        if (lastWord.Length > wordPattern.Length)
                        {
                            // skip until same length
                            continue;
                        }
                        else if (lastWord.Length < wordPattern.Length)
                        {
                            // this means we are continuing with longer patterns
                            // and should process as normal
                            hasFoundWord = true;
                        }
                        else
                        {
                            // same length so compare
                            var patternRegexp = wordPattern.Replace('?', '.');
                            Match match = Regex.Match(lastWord, patternRegexp, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                // found
                                hasFoundWord = true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (hasFoundWord)
                    {
                        ReadWordsByWordPattern(wordPattern, driver, db, user);
                    }
                }
                else
                {
                    ReadWordsByWordPattern(wordPattern, driver, db, user);
                }
            }
        }

        private void ReadWordsByWordPattern(string wordPattern, IWebDriver driver, WordHintDbContext db, User user)
        {
            // go to search page            
            string url = "https://kryssordhjelp.no/";
            driver.Navigate().GoToUrl(url);

            if (user == null)
            {
                // set admin user
                user = new User()
                {
                    FirstName = "",
                    LastName = "Kryssordhjelp",
                    UserName = "kryssordhjelp",
                    isVIP = true
                };

                db.DictionaryUsers.Add(user);
                db.SaveChanges();
            }

            Log.Information("Processing pattern search for '{0}'", wordPattern);
            writer.WriteLine("Processing pattern search for '{0}'", wordPattern);

            // select the drop down list
            var lengthElement = driver.FindElement(By.Name("length"));

            // create select element object 
            var selectElement = new SelectElement(lengthElement);

            // select by value
            selectElement.SelectByValue(wordPattern.Length.ToString());

            // select the letter fields
            var letter1 = driver.FindElement(By.Name("letter[1]"));
            letter1.SendKeys(wordPattern[0].ToString());

            // find submit button
            var login = driver.FindElement(By.Id("submitsearch"));
            login.Click();

            driver.WaitForElementLoad(By.Id("wordlist"), 20);

            // parse all words
            IList<IWebElement> listElements = driver.FindElements(By.XPath("//ul[@class='word']/li"));
            IWebElement ahref = null;
            foreach (IWebElement listElement in listElements)
            {
                ahref = listElement.FindElement(By.TagName("a"));
                var wordText = ahref.Text;
                var href = ahref.GetAttribute("href");

                var word = new Word
                {
                    Language = "no",
                    Value = wordText,
                    NumberOfLetters = wordText.Count(c => c != ' '),
                    NumberOfWords = CountNumberOfWords(wordText),
                    User = user,
                    CreatedDate = DateTime.Now
                };

                // check if word already exists
                var existingWord = db.Words.Where(o => o.Value == wordText).FirstOrDefault();
                if (existingWord != null)
                {
                    // update reference to existing word (reuse the word)
                    word = existingWord;
                }
                else
                {
                    // add new word
                    db.Words.Add(word);
                    db.SaveChanges();
                }

                GetWordSynonyms(word, driver, db, user, href);
            }
        }

        private void GetWordSynonyms(Word word, IWebDriver driver, WordHintDbContext db, User user, string url)
        {
            // there is a bug in the website that makes a  query with "0" fail
            if (word.Value == "0") return;

            // open a new tab and set the context
            var chromeDriver = (ChromeDriver)driver;

            // save a reference to our original tab's window handle
            var originalTabInstance = chromeDriver.CurrentWindowHandle;

            // execute some JavaScript to open a new window
            chromeDriver.ExecuteScript("window.open();");

            // save a reference to our new tab's window handle, this would be the last entry in the WindowHandles collection
            var newTabInstance = chromeDriver.WindowHandles[driver.WindowHandles.Count - 1];

            // switch our WebDriver to the new tab's window handle
            chromeDriver.SwitchTo().Window(newTabInstance);

            // lets navigate to a web site in our new tab
            driver.Navigate().GoToUrl(url);

            Log.Information("Processing synonym search for '{0}'", word.Value);
            writer.WriteLine("Processing synonym search for '{0}'", word.Value);

            // parse all synonyms
            // parse all words
            IList<IWebElement> listElements = driver.FindElements(By.XPath("//ul[@class='word']/li"));
            IWebElement ahref = null;
            foreach (IWebElement listElement in listElements)
            {
                ahref = listElement.FindElement(By.TagName("a"));
                var hintText = ahref.Text;
                var href = ahref.GetAttribute("href");

                var hint = new Hint
                {
                    Language = "no",
                    Value = hintText,
                    NumberOfLetters = hintText.Count(c => c != ' '),
                    NumberOfWords = CountNumberOfWords(hintText),
                    User = user,
                    CreatedDate = DateTime.Now
                };

                // check if hint already exists
                bool skipHint = false;
                var existingHint = db.Hints
                                    .Include(h => h.WordHints)
                                    .Where(o => o.Value == hintText).FirstOrDefault();
                if (existingHint != null)
                {
                    // update reference to existing hint (reuse the hint)
                    hint = existingHint;

                    // check if the current word already has been added as a reference to this hint
                    if (hint.WordHints.Count(h => h.WordId == word.WordId) > 0)
                    {
                        skipHint = true;
                    }
                }
                else
                {
                    // add new hint
                    db.Hints.Add(hint);
                }

                if (!skipHint)
                {
                    word.WordHints.Add(new WordHint()
                    {
                        Word = word,
                        Hint = hint
                    });

                    db.SaveChanges();

                    Log.Debug("Added '{0}' as a hint for '{1}'", hintText, word.Value);
                    writer.WriteLine("Added '{0}' as a hint for '{1}'", hintText, word.Value);
                }
                else
                {
                    Log.Debug("Skipped adding '{0}' as a hint for '{1}' ...", hintText, word.Value);
                    writer.WriteLine("Skipped adding '{0}' as a hint for '{1}' ...", hintText, word.Value);
                }
            }

            // now lets close our new tab
            chromeDriver.ExecuteScript("window.close();");

            // and switch our WebDriver back to the original tab's window handle
            chromeDriver.SwitchTo().Window(originalTabInstance);

            // and have our WebDriver focus on the main document in the page to send commands to 
            chromeDriver.SwitchTo().DefaultContent();
        }

        public static int CountNumberOfWords(string text)
        {
            char[] delimiters = new char[] { ' ', '\r', '\n' };
            return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}