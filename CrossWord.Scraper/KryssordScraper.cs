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
    public class KryssordScraper
    {
        TextWriter writer = null;
        string connectionString = null;
        string signalRHubURL = null;
        string source = null;

        public KryssordScraper(string connectionString, string signalRHubURL, string siteUsername, string sitePassword, int letterCount, bool doContinueWithLastWord = true)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "kryssord.org";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(signalRHubURL, letterCount.ToString());
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            // make sure that no chrome and chrome drivers are running
            // cannot do this here, since several instances of the scraper might be running in parallel
            // do this before this class is called instead
            // KillAllChromeDriverInstances();

            DoScrape(siteUsername, sitePassword, letterCount, source, doContinueWithLastWord);
        }

        private void DoScrape(string siteUsername, string sitePassword, int letterCount, string source, bool doContinueWithLastWord)
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
                        case 3:
                            lastWordString = "aaa";
                            break;
                        default:
                            lastWordString = "aaa" + new string('?', letterCount - 3);
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

                using (var driver = ChromeDriverUtils.GetChromeDriver(true))
                {
                    DoLogon(driver, siteUsername, sitePassword);

                    // read all one letter words
                    if (lastWordString == null || lastWordString != null && letterCount == 1)
                    {
                        ReadWordsByWordPattern("1", driver, db, adminUser);
                    }

                    // read all two letter words
                    if (lastWordString == null || lastWordString != null && letterCount == 2)
                    {
                        ReadWordsByWordPermutations(2, 2, driver, db, adminUser, lastWordString);
                    }

                    // read 3 and more letter words
                    for (int i = 3; i < 200; i++)
                    {
                        // added break to support several docker instances scraping in swarms
                        if (i > lastWordString.Length)
                        {
                            Log.Error("Warning! Quitting since the current letter length > letter count: {0} / {1}", i, letterCount);
                            break;
                        }

                        ReadWordsByWordPermutations(3, i, driver, db, adminUser, lastWordString);
                    }
                }
            }
        }

        private void DoLogon(IWebDriver driver, string siteUsername, string sitePassword)
        {
            driver.Navigate().GoToUrl("https://www.kryssord.org/login.php");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            var ready = wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            // login if login form is present
            if (driver.IsElementPresent(By.XPath("//input[@name='username']"))
                && driver.IsElementPresent(By.XPath("//input[@name='password']")))
            {
                IWebElement username = driver.FindElement(By.XPath("//input[@name='username']"));
                IWebElement password = driver.FindElement(By.XPath("//input[@name='password']"));

                username.Clear();
                username.SendKeys(siteUsername);

                password.Clear();
                password.SendKeys(sitePassword);

                // use password field to submit form
                password.Submit();

                var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                var ready2 = wait2.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
            }
        }

        private void ReadWordsByWordPermutations(int permutationSize, int letterLength, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
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
                if (letterLength > permutationSize)
                {
                    // make word search pattern                    
                    wordPattern = permutation.PadRight(letterLength, '?');
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
            // go to search result page            
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                Log.Information("Processing pattern search for '{0}' on page {1}", wordPattern, page + 1);
                writer.WriteLine("Processing pattern search for '{0}' on page {1}", wordPattern, page + 1);

                // parse total number of words found
                var wordCountElement = driver.FindElementOrNull(By.XPath("/html/body//div[@id='content']/h1/strong"));

                if (wordCountElement == null) break;
                var wordCount = wordCountElement.Text;

                // return if nothing was found
                if (wordCount == "0")
                {
                    return;
                }
                else
                {
                    var isNumeric = int.TryParse(wordCount, out int n);
                    if (isNumeric)
                    {
                        Log.Information("Found {0} words when searching for '{1}' on page {2}", n, wordPattern, page + 1);
                        writer.WriteLine("Found {0} words when searching for '{1}' on page {2}", n, wordPattern, page + 1);

                        if (n > 108) Log.Error("Warning! Pattern search for '{0}' on page {1} has too many words: {2}", wordPattern, page + 1, n);
                    }
                }

                // parse all words
                // var words = ReadWords(driver, adminUser);
                var words = ReadWordsAgilityPack(driver, adminUser);
                foreach (var word in words)
                {
                    GetWordSynonyms(word, driver, db, adminUser);
                }

                // go to next page if exist
                var nextPageElement = FindNextPageOrNull(driver);
                if (nextPageElement != null)
                {
                    // nextPageElement.Click();
                    var nextPageUrl = nextPageElement.GetParent().GetAttribute("href");
                    var urlParams = ExtractUrlParameters(nextPageUrl);
                    page = urlParams.Item3;
                    driver.Navigate().GoToUrl(nextPageUrl);
                }
                else
                {
                    break;
                }
            }
        }

        private void GetWordSynonyms(Word word, IWebDriver driver, WordHintDbContext db, User adminUser)
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
            var wordPattern = "";
            var query = ScraperUtils.EscapeUrlString(word.Value);
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);
                writer.WriteLine("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);

                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

                // return if nothing was found
                if (wordCount == "0")
                {
                    break;
                }
                else
                {
                    var isNumeric = int.TryParse(wordCount, out int n);
                    if (isNumeric)
                    {
                        Log.Information("Found {0} synonyms when searching for '{1}' on page {2}", n, word.Value, page + 1);
                        writer.WriteLine("Found {0} synonyms when searching for '{1}' on page {2}", n, word.Value, page + 1);


                        if (n > 108) Log.Error("Warning! synonym search for '{0}' on page {1} has too many words: {2}", word.Value, page + 1, n);
                    }
                }

                // parse all related words
                // var relatedWords = ReadRelatedWords(driver, adminUser);
                var relatedWords = ReadRelatedWordsAgilityPack(driver, adminUser);

                // and add to database
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer);

                // go to next page if exist
                var nextPageElement = FindNextPageOrNull(driver);
                if (nextPageElement != null)
                {
                    var nextPageUrl = nextPageElement.GetParent().GetAttribute("href");
                    var urlParams = ExtractUrlParameters(nextPageUrl);
                    page = urlParams.Item3;
                    driver.Navigate().GoToUrl(nextPageUrl);
                }
                else
                {
                    break;
                }
            }

            // now lets close our new tab
            chromeDriver.ExecuteScript("window.close();");

            // and switch our WebDriver back to the original tab's window handle
            chromeDriver.SwitchTo().Window(originalTabInstance);

            // and have our WebDriver focus on the main document in the page to send commands to 
            chromeDriver.SwitchTo().DefaultContent();
        }

        private IList<Word> ReadWords(IWebDriver driver, User adminUser)
        {
            IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
            IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
            IList<IWebElement> rowTD;

            var wordListing = new List<Word>();
            foreach (IWebElement row in tableRow)
            {
                rowTD = row.FindElements(By.TagName("td"));
                var wordText = rowTD[0].Text.ToUpper();
                var userId = rowTD[3].Text;
                var date = rowTD[4].Text;

                var word = new Word
                {
                    Language = "no",
                    Value = wordText,
                    NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(wordText),
                    User = adminUser,
                    CreatedDate = ScraperUtils.ParseDateTimeOrNow(date, "yyyy-MM-dd"),
                    Source = this.source,
                    Comment = "User " + userId
                };

                wordListing.Add(word);
            }

            return wordListing;
        }

        private IList<Word> ReadWordsAgilityPack(IWebDriver driver, User adminUser)
        {
            var tableRows = driver.FindNodes(By.XPath("/html/body//div[@class='results']/table/tbody/tr"));

            var wordListing = new List<Word>();
            foreach (var row in tableRows)
            {
                var rowTD = row.FindNodes(By.TagName("td"));

                var wordText = rowTD[0].InnerText.Trim().ToUpper();
                var userId = rowTD[3].InnerText.Trim();
                var date = rowTD[4].InnerText.Trim();

                var word = new Word
                {
                    Language = "no",
                    Value = wordText,
                    NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(wordText),
                    User = adminUser,
                    CreatedDate = ScraperUtils.ParseDateTimeOrNow(date, "yyyy-MM-dd"),
                    Source = this.source,
                    Comment = "User " + userId
                };

                wordListing.Add(word);
            }

            return wordListing;
        }

        private IList<Word> ReadRelatedWords(IWebDriver driver, User adminUser)
        {
            // parse all related words
            IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
            IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
            IList<IWebElement> rowTD;

            var relatedWords = new List<Word>();
            foreach (IWebElement row in tableRow)
            {
                rowTD = row.FindElements(By.TagName("td"));
                var hintText = rowTD[0].Text.ToUpper();
                var userId = rowTD[3].Text;
                var date = rowTD[4].Text;

                var hint = new Word
                {
                    Language = "no",
                    Value = hintText,
                    NumberOfLetters = ScraperUtils.CountNumberOfLetters(hintText),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(hintText),
                    User = adminUser,
                    CreatedDate = ScraperUtils.ParseDateTimeOrNow(date, "yyyy-MM-dd"),
                    Source = this.source,
                    Comment = "User " + userId
                };

                relatedWords.Add(hint);
            }

            relatedWords = relatedWords.Distinct().ToList(); // Note that this requires the object to implement IEquatable<Word> 

            return relatedWords;
        }

        private IList<Word> ReadRelatedWordsAgilityPack(IWebDriver driver, User adminUser)
        {
            // parse all related words
            var tableRows = driver.FindNodes(By.XPath("/html/body//div[@class='results']/table/tbody/tr"));

            var relatedWords = new List<Word>();
            foreach (var row in tableRows)
            {
                var rowTD = row.FindNodes(By.TagName("td"));
                var hintText = rowTD[0].InnerText.Trim().ToUpper();
                var userId = rowTD[3].InnerText.Trim();
                var date = rowTD[4].InnerText.Trim();

                var hint = new Word
                {
                    Language = "no",
                    Value = hintText,
                    NumberOfLetters = ScraperUtils.CountNumberOfLetters(hintText),
                    NumberOfWords = ScraperUtils.CountNumberOfWords(hintText),
                    User = adminUser,
                    CreatedDate = ScraperUtils.ParseDateTimeOrNow(date, "yyyy-MM-dd"),
                    Source = this.source,
                    Comment = "User " + userId
                };

                relatedWords.Add(hint);
            }

            relatedWords = relatedWords.Distinct().ToList(); // Note that this requires the object to implement IEquatable<Word> 

            return relatedWords;
        }

        private static IWebElement FindNextPageOrNull(IWebDriver driver)
        {
            return driver.FindElementOrNull(By.XPath("//div[@class='pages']/ul/li/a/span[contains(., 'Neste')]"));
        }

        private static Tuple<string, string, int> ExtractUrlParameters(string url)
        {
            // https://www.kryssord.org/search.php?a=10&b=&p=0
            string word = "";
            string wordPattern = "";
            int page = 0;

            Regex regexObj = new Regex(@"a=(.*)&b=(.*)&p=(\d*)", RegexOptions.IgnoreCase);
            Match matchResults = regexObj.Match(url);
            if (matchResults.Success)
            {
                word = matchResults.Groups[1].Value;
                wordPattern = matchResults.Groups[2].Value;
                page = matchResults.Groups[3].Value == "" ? 0 : int.Parse(matchResults.Groups[3].Value);
                return new Tuple<string, string, int>(word, wordPattern, page);
            }

            return null;
        }
    }
}