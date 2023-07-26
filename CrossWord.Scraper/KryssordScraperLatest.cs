using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System.Timers;
using System.Threading;
using CrossWord.Scraper.Extensions;

namespace CrossWord.Scraper
{
    public class KryssordScraperLatest
    {
        private readonly TextWriter writer = null;
        private readonly string connectionString = null;
        private readonly string signalRHubURL = null;
        private readonly string source = null;

        public KryssordScraperLatest(string connectionString, string signalRHubURL, string siteUsername, string sitePassword, int kryssordLatestDelaySeconds)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "kryssord.org-latest";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(this.signalRHubURL, this.source);
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            // make sure that no chrome and chrome drivers are running
            // cannot do this here, since several instances of the scraper might be running in parallel
            // do this before this class is called instead
            // KillAllChromeDriverInstances();

            // run forever
            while (true)
            {
                DoScrape(siteUsername, sitePassword, source);

                Thread.Sleep(kryssordLatestDelaySeconds * 1000);
            }
        }

        private void DoScrape(string siteUsername, string sitePassword, string source)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
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
                    DoLogon(driver, siteUsername, sitePassword);

                    string url = "https://www.kryssord.org";
                    driver.Navigate().GoToUrl(url);
                    var documentNode = driver.GetDocumentNode();
                    ProcessWordsUntilEmpty(driver, db, adminUser, documentNode);
                }
            }
        }

        private static void DoLogon(IWebDriver driver, string siteUsername, string sitePassword)
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

        private void ProcessWordsUntilEmpty(IWebDriver driver, WordHintDbContext db, User adminUser, HtmlNode documentNode)
        {
            Log.Information("Looking for latest words and synonyms on frontpage");
            writer.WriteLine("Looking for latest words and synonyms on frontpage");

            // parse all words
            var wordAndHrefs = ParseWordsAgilityPack(documentNode, adminUser);

            foreach (var wordAndHref in wordAndHrefs)
            {
                var wordText = wordAndHref.Item1;
                var url = wordAndHref.Item2;

                Log.Information("Processing word found on frontpage: {0}, {1}", wordText, url);
                writer.WriteLine("Processing word found on frontpage: {0}, {1}", wordText, url);

                var word = ReadWordByWordText(driver, adminUser, wordText);
                if (word != null)
                {
                    GetWordSynonyms(word, driver, db, adminUser);
                }
            }
        }

        private Word ReadWordByWordText(IWebDriver driver, User adminUser, string wordText)
        {
            string url = $"https://www.kryssord.org/search.php?a=&b={wordText}";
            driver.Navigate().GoToUrl(url);
            var documentNode = driver.GetDocumentNode();
            var words = ReadWordsAgilityPack(documentNode, adminUser);
            return words.FirstOrDefault();
        }

        private IList<Word> ReadWordsAgilityPack(HtmlNode node, User adminUser)
        {
            var tableRows = node.FindNodes(By.XPath("/html/body//div[@class='results']/table/tbody/tr"));

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

            var (count, documentNode) = GetWordCountByWordPattern(driver, url);
            if (count == 0)
            {
                return;
            }
            else
            {
                Log.Information("Found {0} synonyms when searching for '{1}' on page {2}", count, word.Value, page + 1);
                writer.WriteLine("Found {0} synonyms when searching for '{1}' on page {2}", count, word.Value, page + 1);

                if (count > 108)
                {
                    Log.Error("Warning! synonym search for '{0}' on page {1} has too many words: {2}", word.Value, page + 1, count);
                }
            }

            ProcessSynonymsUntilEmpty(word, driver, db, adminUser, page, documentNode, url);

            // now lets close our new tab
            chromeDriver.ExecuteScript("window.close();");

            // and switch our WebDriver back to the original tab's window handle
            chromeDriver.SwitchTo().Window(originalTabInstance);

            // and have our WebDriver focus on the main document in the page to send commands to 
            chromeDriver.SwitchTo().DefaultContent();
        }

        private static Tuple<int, HtmlNode> GetWordCountByWordPattern(IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(url);

            // parse total number of words found
            var documentNode = driver.GetDocumentNode();
            var wordCountElement = documentNode.FindNode(By.XPath("/html/body//div[@id='content']/h1/strong"));

            // return if nothing was found
            if (wordCountElement == null)
            {
                return new Tuple<int, HtmlNode>(0, documentNode);
            }

            var wordCount = wordCountElement.InnerText.Trim();

            // return if nothing was found
            if (wordCount == "0")
            {
                return new Tuple<int, HtmlNode>(0, documentNode);
            }
            else
            {
                var isNumeric = int.TryParse(wordCount, out int n);
                if (isNumeric)
                {
                    return new Tuple<int, HtmlNode>(n, documentNode);
                }
            }

            return new Tuple<int, HtmlNode>(0, documentNode);
        }

        private void ProcessSynonymsUntilEmpty(Word word, IWebDriver driver, WordHintDbContext db, User adminUser, int page, HtmlNode documentNode, string url)
        {
            while (true)
            {
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);
                writer.WriteLine("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);

                // parse all related words
                var relatedWords = ReadRelatedWordsAgilityPack(documentNode, adminUser);

                // and add to database
                // don't update state
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer, false);

                // go to next page if exist
                // Note! this only works if we are logged in
                var (hasFoundNextPage, pageNumber, pageUrl, pageNode) = NavigateToNextPageIfExist(driver, documentNode);
                if (hasFoundNextPage)
                {
                    url = pageUrl;
                    page = pageNumber;
                    documentNode = pageNode;
                }
                else
                {
                    break;
                }
            }
        }

        private static Tuple<bool, int, string, HtmlNode> NavigateToNextPageIfExist(IWebDriver driver, HtmlNode node)
        {
            var nextPageElement = node.FindNode(By.XPath("//div[@class='pages']/ul/li/a/span[contains(., 'Neste')]"));
            if (nextPageElement != null)
            {
                var nextPageUrl = nextPageElement.ParentNode.Attributes["href"].Value;
                var decodedeNextPageUrl = WebUtility.UrlDecode(nextPageUrl);
                // fix some issues with the url decoding
                decodedeNextPageUrl = decodedeNextPageUrl.Replace("&amp;", "&");
                decodedeNextPageUrl = decodedeNextPageUrl.Replace("&quot;", "%22");

                // set page number
                var urlParams = ExtractUrlParameters(decodedeNextPageUrl);
                int pageNumber = urlParams.Item3;

                // append root: https://www.kryssord.org
                decodedeNextPageUrl = "https://www.kryssord.org" + decodedeNextPageUrl;
                driver.Navigate().GoToUrl(decodedeNextPageUrl);

                var documentNode = driver.GetDocumentNode();

                return new Tuple<bool, int, string, HtmlNode>(true, pageNumber, decodedeNextPageUrl, documentNode);
            }
            else
            {
                return new Tuple<bool, int, string, HtmlNode>(false, -1, null, null);
            }
        }

        private List<(string, string)> ParseWordsAgilityPack(HtmlNode doc, User adminUser)
        {
            var wordAndHrefs = new List<(string, string)>();

            // https://www.kryssord.org/
            var ahrefs = doc.FindNodes(By.XPath("//td[@class='word']//a[starts-with(@href, '/search.php?a=')]"));
            if (ahrefs == null) return wordAndHrefs;

            foreach (var ahref in ahrefs)
            {
                var wordText = ahref.InnerText.Trim().ToUpper();
                var href = ahref.Attributes["href"].Value;
                string url = $"https://www.kryssord.org{href}";

                if (!string.IsNullOrEmpty(wordText)) wordAndHrefs.Add((wordText, url));
            }

            wordAndHrefs = wordAndHrefs.Distinct().ToList(); // Note that this requires the object to implement IEquatable<> 
            return wordAndHrefs;
        }

        private IList<Word> ReadRelatedWordsAgilityPack(HtmlNode node, User adminUser)
        {
            // parse all related words
            var tableRows = node.FindNodes(By.XPath("/html/body//div[@class='results']/table/tbody/tr"));

            var relatedWords = new List<Word>();
            if (tableRows == null) return relatedWords;

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