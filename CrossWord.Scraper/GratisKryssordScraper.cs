using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;

namespace CrossWord.Scraper
{
    public class GratisKryssordScraper
    {
        TextWriter writer = null;
        string connectionString = null;
        string signalRHubURL = null;
        string source = null;

        public GratisKryssordScraper(string connectionString, string signalRHubURL, int startLetterCount, int endLetterCount, bool doContinueWithLastWord)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "gratiskryssord.no";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(signalRHubURL, startLetterCount.ToString());
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            // make sure that no chrome and chrome drivers are running
            // cannot do this here, since several instances of the scraper might be running in parallel
            // do this before this class is called instead
            // KillAllChromeDriverInstances();

            DoScrape(startLetterCount, endLetterCount, source, doContinueWithLastWord);
        }

        private void DoScrape(int startLetterCount, int endLetterCount, string source, bool doContinueWithLastWord)
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

                using (var driver = ChromeDriverUtils.GetChromeDriver(true))
                {
                    // set general timeout to long
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(180);

                    // read all words with the letter count
                    ReadWordsByWordPermutations(startLetterCount, endLetterCount, driver, db, adminUser, doContinueWithLastWord);
                }
            }
        }

        private void ReadWordsByWordPermutations(int startLetterCount, int endLetterCount, IWebDriver driver, WordHintDbContext db, User adminUser, bool doContinueWithLastWord)
        {
            var alphabet = "abcdefghijklmnopqrstuvwxyzåæøö";
            var permutations = alphabet.Select(x => x.ToString());
            int permutationSize = 2;

            for (int i = 0; i < permutationSize - 1; i++)
            {
                permutations = permutations.SelectMany(x => alphabet, (x, y) => x + y);
            }

            var wordPermutationList = permutations.ToList();
            wordPermutationList.Add("&");
            wordPermutationList.Add("(");
            wordPermutationList.Add(")");
            wordPermutationList.Add("+");
            wordPermutationList.Add(",");
            wordPermutationList.Add("-");
            wordPermutationList.Add("0");
            wordPermutationList.Add("1");
            wordPermutationList.Add("2");
            wordPermutationList.Add("3");
            wordPermutationList.Add("4");
            wordPermutationList.Add("5");
            wordPermutationList.Add("6");
            wordPermutationList.Add("7");
            wordPermutationList.Add("8");
            wordPermutationList.Add("9");

            // use the letter count a little bit different when it comes to the alphabetic index:
            // letterCount is the index to start with divided out on the total alphabetic index
            // e.g. 
            // if letter count is between 1 - 4 of a total index length of 1000:
            // 1 is 1
            // 2 is 250
            // 3 is 500
            // 4 is 750
            int length = wordPermutationList.Count;
            int startIndex = (int)(((double)length / (double)endLetterCount) * (startLetterCount - 1));
            int endIndex = (int)((((double)length / (double)endLetterCount) * startLetterCount) - 1);
            var startString = wordPermutationList[startIndex];
            var endString = wordPermutationList[endIndex];

            Log.Information("Processing alphabetic permutation search using {0}-{1} = {2}-{3} ({4} - {5}) ", startLetterCount, endLetterCount, startIndex, endIndex, startString, endString);
            writer.WriteLine("Processing alphabetic permutation search using {0}-{1} = {2}-{3} ({4} - {5}) ", startLetterCount, endLetterCount, startIndex, endIndex, startString, endString);

            // add some extra status information to the writer
            if (this.writer is SignalRClientWriter)
            {
                (this.writer as SignalRClientWriter).ExtraStatusInformation = string.Format("Processing alphabetic permutation search using {0}-{1} = {2}-{3} ({4} - {5}) ", startLetterCount, endLetterCount, startIndex, endIndex, startString, endString);
            }

            int curIndex = 0;
            foreach (var wordPermutation in wordPermutationList)
            {
                string wordPattern = wordPermutation.Length == 1 && wordPermutation[0] < 45 ? string.Format("%{0:X}", (int)wordPermutation[0]) : wordPermutation;
                curIndex++;

                if (curIndex < startIndex + 1)
                {
                    Log.Information("Skipping pattern '{0}' until we reach index {1}: '{2}'. [{3}/{4}]", wordPattern, startIndex, startString, curIndex, length);
                    writer.WriteLine("Skipping pattern '{0}' until we reach index {1}: '{2}'. [{3}/{4}]", wordPattern, startIndex, startString, curIndex, length);
                    continue;
                }
                else if (length != curIndex && curIndex == endIndex + 1) // stop at last index except very last character
                {
                    // reached the end - quit
                    Log.Information("Quitting because we have reached the last index to process: {0} at index {1}.", wordPattern, curIndex);
                    writer.WriteLine("Quitting because we have reached the last index to process: {0} at index {1}.", wordPattern, curIndex);
                    break;
                }

                string lastWordString = null;
                if (doContinueWithLastWord)
                {
                    lastWordString = WordDatabaseService.GetLastWordFromComment(db, source, wordPattern);
                }

                // var href = $"https://www.gratiskryssord.no/kryssordbok/?kart={wordPattern}#oppslag";
                var href = $"https://www.gratiskryssord.no/kryssordbok/alfabetisk/{wordPattern}/";
#if DEBUG
                // if (wordPermutation == "xå")
                // {
                //     wordPattern = "kå";
                //     href = $"https://www.gratiskryssord.no/kryssordbok/?kart={wordPattern}#oppslag";
                //     lastWordString = WordDatabaseService.GetLastWordFromComment(db, source, wordPattern);
                // }
                // else if (wordPermutation == "&")
                // {
                //     // debugging - break here
                // }

                wordPattern = "na";
                href = $"https://www.gratiskryssord.no/kryssordbok/alfabetisk/{wordPattern}/";
                lastWordString = "NAVN";
#endif
                ReadWordsByWordUrl(wordPattern, href, driver, db, adminUser, lastWordString);
            }
        }

        private void ReadWordsByWordUrl(string wordPrefix, string url, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            // go to word page            
            try
            {
                driver.Navigate().GoToUrl(url);
            }
            catch (System.Exception)
            {
                // Log.Error("Timeout navigating to '{0}'", url);
                writer.WriteLine("Timeout navigating to '{0}'", url);
                return;
            }

            Log.Information("Processing word search for '{0}'", wordPrefix);
            writer.WriteLine("Processing word search for '{0}'", wordPrefix);

            // read the whole document into a HtmlNode
            HtmlNode doc = driver.GetDocumentNode();

            // and parse using agility pack
            var words = ParseWordsAgilityPack(doc, adminUser);

            bool doSkip = true;
            foreach (var wordAndHref in words)
            {
                var word = wordAndHref.Item1;
                var href = wordAndHref.Item2;
                var wordText = word.Value;

                // skip until we get to the last word
                if (doSkip && lastWord != null && lastWord != wordText)
                {
                    Log.Information("Skipping alphabetic word '{0}' until we find '{1}'", wordText, lastWord);
                    writer.WriteLine("Skipping alphabetic word '{0}' until we find '{1}'", wordText, lastWord);
                    continue;
                }
                doSkip = false; // make sure we don't skip on the next word after we have skipped

                // update that we are processing this word
                WordDatabaseService.UpdateState(db, source, new Word() { Value = wordText, Comment = wordPrefix, CreatedDate = DateTime.Now }, writer, true);

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
            // https://www.gratiskryssord.no/kryssordbok/navn/side/1/
            var page = 1;
            var pageUrl = $"{url}side/{page}/";
            driver.Navigate().GoToUrl(pageUrl);
            while (true)
            {
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page);
                writer.WriteLine("Processing synonym search for '{0}' on page {1}", word.Value, page);

                // read the whole document into a HtmlNode
                HtmlNode doc = driver.GetDocumentNode();

                // and parse synonyms using Agility Pack
                var relatedWords = ParseSynonymsAgilityPack(word, doc, adminUser);

                // and add to database
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer, false);

                // go to next page if exist
                var nextPageElement = FindNextPageOrNull(doc, word.Value.ToLower(), page + 1);
                if (nextPageElement != null)
                {
                    var hintText = nextPageElement.InnerText.Trim().ToUpper();
                    hintText = HttpUtility.HtmlDecode(hintText); // ensure that text like &amp; gets converted to &
                    var href = nextPageElement.Attributes["href"].Value;
                    string nextPageUrl = $"https://www.gratiskryssord.no{href}";

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

        private List<(Word, string)> ParseWordsAgilityPack(HtmlNode doc, User adminUser)
        {
            var words = new List<(Word, string)>();

            // https://www.gratiskryssord.no/kryssordbok/alfabetisk/aa/
            var ahrefs = doc.FindNodes(By.XPath("//div[@id='staticPage']//a[starts-with(@href, '/kryssordbok/')]"));
            if (ahrefs == null) return words;

            foreach (var ahref in ahrefs)
            {
                var wordText = ahref.InnerText.Trim().ToUpper();
                wordText = HttpUtility.HtmlDecode(wordText); // ensure that text like &amp; gets converted to &
                var href = ahref.Attributes["href"].Value;
                string url = $"https://www.gratiskryssord.no{href}";

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

                words.Add((word, url));
            }

            return words;
        }

        private List<Word> ParseSynonymsAgilityPack(Word word, HtmlNode doc, User adminUser)
        {
            // parse all synonyms
            var relatedWords = new List<Word>();

            // https://www.gratiskryssord.no/kryssordbok/
            var ahrefs = doc.FindNodes(By.XPath("//div[@id='staticPage']//a[starts-with(@href, '/kryssordbok/')]"));
            if (ahrefs == null) return relatedWords;

            foreach (var ahref in ahrefs)
            {
                var hintText = ahref.InnerText.Trim().ToUpper();
                hintText = HttpUtility.HtmlDecode(hintText); // ensure that text like &amp; gets converted to &
                var href = ahref.Attributes["href"].Value;
                // string url = $"https://www.gratiskryssord.no{href}";

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

        private static HtmlNode FindNextPageOrNull(HtmlNode doc, string wordText, int page)
        {
            // /kryssordbok/navn/side/2
            string endUrl = $"/side/{page}";
            return doc.FindNode(By.XPath($"//a[contains(@href, '{endUrl}')]"));
        }
    }
}