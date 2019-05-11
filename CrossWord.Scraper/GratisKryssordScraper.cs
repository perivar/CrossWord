using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace CrossWord.Scraper
{
    public class GratisKryssordScraper
    {
        TextWriter writer = null;
        string connectionString = null;
        string signalRHubURL = null;
        string source = null;

        public GratisKryssordScraper(string connectionString, string signalRHubURL, int letterCount, bool doContinueWithLastWord = true)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "gratiskryssord.no";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(signalRHubURL, letterCount.ToString());
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
                    // read all words with the letter count
                    ReadWordsByAlphabeticOverview(letterCount, driver, db, adminUser, lastWordString);
                }
            }
        }

        private void ReadWordsByAlphabeticOverview(int letterCount, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            // go to alphabetic overview            
            string url = "https://www.gratiskryssord.no/kryssordbok/";
            driver.Navigate().GoToUrl(url);

            Log.Information("Processing alphabetic overview for '{0}'", lastWord);
            writer.WriteLine("Processing alphabetic overview for '{0}'", lastWord);

            // wait until the word list has loaded
            try
            {
                // <h5>Alfabetisk oversikt:</h5>
                driver.WaitForElementLoad(By.XPath("//h5['Alfabetisk oversikt:']"), 20);
            }
            catch (System.Exception)
            {
                Log.Error("Timeout searching for '{0}'", lastWord);
                writer.WriteLine("Timeout searching for '{0}'", lastWord);
                return;
            }

            // extract the two first letters of lastWord
            if (lastWord != null)
            {
                lastWord = new string(lastWord.Take(2).ToArray());
            }

#if DEBUG
            lastWord = "BY";
#endif

            // parse all words
            // var wordListing = ParseWordListing(driver);
            var wordListing = ParseWordListingAgilityPack(driver);
            foreach (var wordListElement in wordListing)
            {
                var wordText = wordListElement.Item1;
                var href = wordListElement.Item2;

                // skip until we get to the last word
                if (lastWord != null && wordText != lastWord)
                {
                    Log.Information("Skipping alphabetic word '{0}' until we find '{1}'", wordText, lastWord);
                    // writer.WriteLine("Skipping alphabetic word '{0}' until we find '{1}'", wordText, lastWord);
                    continue;
                }

                ReadWordsByWordPattern(wordText, href, driver, db, adminUser);
            }
        }

        private void ReadWordsByWordPattern(string wordPattern, string url, IWebDriver driver, WordHintDbContext db, User adminUser)
        {
            // go to word page            
            driver.Navigate().GoToUrl(url);

            Log.Information("Processing pattern search for '{0}'", wordPattern);
            writer.WriteLine("Processing pattern search for '{0}'", wordPattern);


            // wait until the word list has loaded
            try
            {
                driver.WaitForElementLoad(By.XPath("//div[@id='oppslag']"), 20);
            }
            catch (System.Exception)
            {
                Log.Error("Timeout searching for '{0}'", wordPattern);
                writer.WriteLine("Timeout searching for '{0}'", wordPattern);
                return;
            }

            // parse all words
            // var words = ParseWords(driver, adminUser);
            var words = ParseWordsAgilityPack(driver, adminUser);

            foreach (var wordAndHref in words)
            {
                var word = wordAndHref.Item1;
                var href = wordAndHref.Item2;

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

            var page = 1;
            while (true)
            {
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page);
                writer.WriteLine("Processing synonym search for '{0}' on page {1}", word.Value, page);

                // parse synonyms
                // var relatedWords = ParseSynonyms(word, driver, adminUser);
                var relatedWords = ParseSynonymsAgilityPack(word, driver, adminUser);

                // and add to database
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer);

                // go to next page if exist
                var nextPageElement = FindNextPageOrNull(driver);
                if (nextPageElement != null)
                {
                    // nextPageElement.Click();
                    var nextPageUrl = nextPageElement.GetAttribute("href");
                    page++;
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

        private List<(string, string)> ParseWordListing(IWebDriver driver)
        {
            // https://www.gratiskryssord.no/kryssordbok/?kart=
            var ahrefs = driver.FindElements(By.XPath("//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?kart=')]"));
            var wordListing = new List<(string, string)>();
            foreach (IWebElement ahref in ahrefs)
            {
                var wordText = ahref.Text;
                var href = ahref.GetAttribute("href");

                wordListing.Add((wordText, href));
            }

            return wordListing;
        }

        private List<(string, string)> ParseWordListingAgilityPack(IWebDriver driver)
        {
            // https://www.gratiskryssord.no/kryssordbok/?kart=
            var ahrefs = driver.FindNodes(By.XPath("//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?kart=')]"));
            var wordListing = new List<(string, string)>();
            foreach (var ahref in ahrefs)
            {
                var wordText = ahref.InnerText;
                var href = ahref.Attributes["href"].Value;

                wordListing.Add((wordText, href));
            }

            return wordListing;
        }


        private List<(Word, string)> ParseWords(IWebDriver driver, User adminUser)
        {
            // https://www.gratiskryssord.no/kryssordbok/?o=
            var ahrefs = driver.FindElements(By.XPath("//div[@id='oppslag']//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?o=')]"));
            var words = new List<(Word, string)>();
            foreach (IWebElement ahref in ahrefs)
            {
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

                words.Add((word, href));
            }

            return words;
        }

        private List<(Word, string)> ParseWordsAgilityPack(IWebDriver driver, User adminUser)
        {
            var words = new List<(Word, string)>();

            // https://www.gratiskryssord.no/kryssordbok/?o=
            var ahrefs = driver.FindNodes(By.XPath("//div[@id='oppslag']//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?o=')]"));
            foreach (var ahref in ahrefs)
            {
                var wordText = ahref.InnerText;
                var href = ahref.Attributes["href"].Value;

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

                words.Add((word, href));
            }

            return words;
        }

        private List<Word> ParseSynonyms(Word word, IWebDriver driver, User adminUser)
        {
            // parse all synonyms
            // https://www.gratiskryssord.no/kryssordbok/?o=
            var ahrefs = driver.FindElements(By.XPath("//div[@class='jscroll-inner']//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?o=')]"));
            var relatedWords = new List<Word>();
            foreach (IWebElement ahref in ahrefs)
            {
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
            return relatedWords;
        }

        private List<Word> ParseSynonymsAgilityPack(Word word, IWebDriver driver, User adminUser)
        {
            // parse all synonyms
            // https://www.gratiskryssord.no/kryssordbok/?o=
            var relatedWords = new List<Word>();

            // https://www.gratiskryssord.no/kryssordbok/?o=
            var ahrefs = driver.FindNodes(By.XPath("//div[@class='jscroll-inner']//a[starts-with(@href, 'https://www.gratiskryssord.no/kryssordbok/?o=')]"));
            foreach (var ahref in ahrefs)
            {
                var hintText = ahref.InnerText;
                var href = ahref.Attributes["href"].Value;

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
            return relatedWords;
        }

        private static IWebElement FindNextPageOrNull(IWebDriver driver)
        {
            string startUrl = $"https://www.gratiskryssord.no/kryssordbok/?o=";
            return driver.FindElementOrNull(By.XPath($"//div[@class='jscroll-inner']//a[starts-with(@href, '{startUrl}')][h1['Vis mer!']]"));
        }

    }
}