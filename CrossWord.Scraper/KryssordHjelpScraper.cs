using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CrossWord.Scraper.Extensions;
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
        private readonly TextWriter writer = null;
        private readonly string connectionString = null;
        private readonly string signalRHubURL = null;
        private readonly string source = null;

        public KryssordHjelpScraper(string connectionString, string signalRHubURL, int letterCount, bool doContinueWithLastWord)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "kryssordhjelp.no";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(this.signalRHubURL, letterCount.ToString());
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            // make sure that no chrome and chrome drivers are running
            // cannot do this here, since several instances of the scraper might be running in parallel
            // do this before this class is called instead
            // KillAllChromeDriverInstances();

            DoScrape(letterCount, source, doContinueWithLastWord);
        }

        private void DoScrape(int letterCount, string source, bool doContinueWithLastWord)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
                string lastWordString = null;
                if (doContinueWithLastWord)
                {
                    lastWordString = WordDatabaseService.GetLastWordFromLetterCount(db, source, letterCount);
                }

                // if we didn't get back a word, use a pattern instead
                if (lastWordString == null)
                {
                    switch (letterCount)
                    {
                        case 1:
                            lastWordString = "a";
                            break;
                        case 2:
                            lastWordString = "aa";
                            break;
                        default:
                            lastWordString = "aa" + new string('?', letterCount - 2);
                            break;
                    }

                    Log.Information("Could not find any words having '{0}' letters. Therefore using last word pattern '{1}'.", letterCount, lastWordString);
                }

                // Note! 
                // the user needs to be added before we disable tracking and disable AutoDetectChanges
                // otherwise this will crash

                // set admin user
                var adminUser = new User()
                {
                    FirstName = "",
                    LastName = "Admin",
                    UserName = "admin"
                };

                // check if user already exists
                var existingUser = db.DictionaryUsers.Where(u => u.FirstName == adminUser.FirstName).FirstOrDefault();
                if (existingUser != null)
                {
                    adminUser = existingUser;
                }
                else
                {
                    db.DictionaryUsers.Add(adminUser);
                    db.SaveChanges();
                }

                // disable tracking to speed things up
                // note that this doesn't load the virtual properties, but loads the object ids after a save
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                // this doesn't seem to work when adding new users all the time
                db.ChangeTracker.AutoDetectChangesEnabled = false;

                using (var driver = ChromeDriverUtils.GetChromeDriver())
                {
                    // read all words with the letter count
                    ReadWordsByWordPermutations(letterCount, driver, db, adminUser, lastWordString);
                }
            }
        }

        private void ReadWordsByWordPermutations(int letterCount, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            int permutationSize = letterCount > 1 ? 2 : 1;

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
                        ReadWordsByWordPattern(wordPattern, driver, db, adminUser);
                    }
                }
                else
                {
                    ReadWordsByWordPattern(wordPattern, driver, db, adminUser);
                }
            }
        }

        private void ReadWordsByWordPattern(string wordPattern, IWebDriver driver, WordHintDbContext db, User adminUser)
        {
            // go to search page            
            string url = "https://kryssordhjelp.no/";
            driver.Navigate().GoToUrl(url);

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
            var letter2 = driver.FindElement(By.Name("letter[2]"));
            letter2.SendKeys(wordPattern[1].ToString());

            // find submit button
            var login = driver.FindElement(By.Id("submitsearch"));
            login.Click();

            // wait until the word list has loaded
            try
            {
                driver.WaitForElementLoad(By.XPath("//div[@id='wordlist']/ul[@class='word']/li"), 20);
            }
            catch (System.Exception)
            {
                Log.Error("Timeout searching for '{0}'", wordPattern);
                writer.WriteLine("Timeout searching for '{0}'", wordPattern);
                return;
            }

            // parse all words
            IList<IWebElement> listElements = driver.FindElements(By.XPath("//div[@id='wordlist']/ul[@class='word']/li"));
            IWebElement ahref = null;
            foreach (IWebElement listElement in listElements)
            {
                try
                {
                    ahref = listElement.FindElement(By.TagName("a"));
                }
                catch (NoSuchElementException)
                {
                    break;
                }

                var wordText = ahref.Text;
                var href = ahref.GetAttribute("href");

                var word = new Word
                {
                    Language = "no",
                    Value = wordText,
                    NumberOfLetters = wordText.Count(c => c != ' '),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(wordText),
                    User = adminUser,
                    CreatedDate = DateTime.Now,
                    Source = this.source
                };

                GetWordSynonyms(word, driver, db, adminUser, href);
            }
        }

        private void GetWordSynonyms(Word word, IWebDriver driver, WordHintDbContext db, User adminUser, string url)
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
            IList<IWebElement> listElements = driver.FindElements(By.XPath("//div[@id='wordlist']/ul[@class='word']/li"));
            IWebElement ahref = null;

            var relatedWords = new List<Word>();
            foreach (IWebElement listElement in listElements)
            {
                try
                {
                    ahref = listElement.FindElement(By.TagName("a"));
                }
                catch (NoSuchElementException)
                {
                    break;
                }

                var hintText = ahref.Text;
                var href = ahref.GetAttribute("href");

                var hint = new Word
                {
                    Language = "no",
                    Value = hintText,
                    NumberOfLetters = hintText.Count(c => c != ' '),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(hintText),
                    User = adminUser,
                    CreatedDate = DateTime.Now,
                    Source = this.source
                };

                relatedWords.Add(hint);
            }

            relatedWords = relatedWords.Distinct().ToList(); // Note that this requires the object to implement IEquatable<Word> 

            // and add to database
            WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer);

            // now lets close our new tab
            chromeDriver.ExecuteScript("window.close();");

            // and switch our WebDriver back to the original tab's window handle
            chromeDriver.SwitchTo().Window(originalTabInstance);

            // and have our WebDriver focus on the main document in the page to send commands to 
            chromeDriver.SwitchTo().DefaultContent();
        }
    }
}