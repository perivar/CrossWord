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

namespace CrossWord.Scraper
{
    public class KryssordScraper
    {
        TextWriter writer = null;
        string connectionString = null;
        string signalRHubURL = null;
        string source = null;
        bool hasFoundPattern = false; // this is the first stage, we match the pattern
        bool hasFoundLastWord = false; // this is the second stage, we not only match the pattern but the word as well
        bool hasMissedLastWord = false; // if we have gone through both stages without finding the last word - then something failed!

        public KryssordScraper(string connectionString, string signalRHubURL, string siteUsername, string sitePassword, int letterCount, bool doContinueWithLastWord)
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

#if DEBUG
                // lastWordString = null;
#endif

                // if we didn't get back a word, use a pattern instead
                if (lastWordString == null)
                {
                    // switch (letterCount)
                    // {
                    //     case 1:
                    //         lastWordString = "a";
                    //         break;
                    //     case 2:
                    //         lastWordString = "aa";
                    //         break;
                    //     case 3:
                    //         lastWordString = "aaa";
                    //         break;
                    //     default:
                    //         lastWordString = "aaa" + new string('?', letterCount - 3);
                    //         break;
                    // }

                    // Log.Information("Could not find any words having '{0}' letters. Therefore using last word pattern '{1}'.", letterCount, lastWordString);

                    hasFoundLastWord = true; // don't skip any words when the last word is empty
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
                    // if (lastWordString == null || lastWordString != null && letterCount == 1)
                    // {
                    //     ReadWordsByWordPattern(new WordPattern("a", 1, 1, lastWordString), driver, db, adminUser);
                    // }

                    // // read all two letter words
                    // if (lastWordString == null || lastWordString != null && letterCount == 2)
                    // {
                    //     ReadWordsByWordPermutations(2, 2, driver, db, adminUser, lastWordString);
                    // }

                    // // read 3 and more letter words
                    // for (int i = 3; i < 200; i++)
                    // {
                    //     // added break to support several docker instances scraping in swarms
                    //     if (i > lastWordString.Length)
                    //     {
                    //         Log.Error("Warning! Quitting since the current letter length > letter count: {0} / {1}", i, letterCount);
                    //         break;
                    //     }

                    //     // ReadWordsByWordPermutations(3, i, driver, db, adminUser, lastWordString);
                    //     ReadWordsByWordPermutations2(i, driver, db, adminUser, lastWordString);
                    // }

                    for (int i = letterCount; i < 200; i++)
                    {
                        // added break to support several docker instances scraping in swarms
                        if (i > letterCount)
                        {
                            Log.Error("Warning! Quitting since the current letter length > letter count: {0} / {1}", i, letterCount);
                            break;
                        }

                        ReadWordsByWordPermutations2(i, driver, db, adminUser, lastWordString);
                    }
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

        private int GetWordCountByPatternDummy(WordPattern wordPattern)
        {
            Random rnd = new Random();
            int randomInt = rnd.Next(1, 10);

            // make every X a too high count 
            if (randomInt == 9)
            {
                return rnd.Next(109, 500);
            }
            else
            {
                return rnd.Next(1, 108);
            }
        }

        private Tuple<int, HtmlNode, string, int> GetWordCountByPattern(IWebDriver driver, WordPattern wordPattern)
        {
            // go to search result page            
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern.Pattern, page);

            var (count, node) = GetWordCountByWordPattern(driver, url);
            return new Tuple<int, HtmlNode, string, int>(count, node, url, page);
        }

        private void ReadWordsByWordPermutations2(int letterLength, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            int wordLength = letterLength;

            int depth = 1;

            // if we are using only one letter, set the depth to zero to force the pattern to become ?
            if (letterLength == 1)
            {
                depth = 0;
            }

            if (lastWord != null)
            {
                wordLength = lastWord.Length;

                // if the word is longer than 3 characters, use the two first letters as pattern
                if (wordLength > 2)
                {
                    depth = 2;
                }
            }

            var permutations = GetPermutations(depth);


            bool hasFound = false;
            foreach (var permutation in permutations)
            {
                if (hasMissedLastWord) return;

                // skip until we reach last word beginning
                if (lastWord != null)
                {
                    if (lastWord.ToLowerInvariant().StartsWith(permutation))
                    {
                        hasFound = true;
                    }
                }
                else
                {
                    hasFound = true;
                }

                if (hasFound) ReadWordsByWordPermutationsRecursive(driver, new WordPattern(permutation, wordLength, depth, lastWord), db, adminUser);
            }
        }

        // recursive function that progressively increases the depth if we get a too high word count
        // but maximum 3 permutations (abc???)
        private void ReadWordsByWordPermutationsRecursive(IWebDriver driver, WordPattern wordPattern, WordHintDbContext db, User adminUser)
        {
            // get word count
            var (wordCount, node, url, page) = GetWordCountByPattern(driver, wordPattern);

            if (wordCount == 0)
            {
                return;
            }
            else
            {
                Log.Information("Found {0} words when searching for '{1}' on page {2}", wordCount, wordPattern.Pattern, page + 1);
                writer.WriteLine("Found {0} words when searching for '{1}' on page {2}", wordCount, wordPattern.Pattern, page + 1);
            }

            // if we get too many words back, try to increase the pattern depth
            // but maximum 3 levels
            if (wordCount <= 108 || wordPattern.Depth > 3)
            {
                if (wordPattern.Depth > 3)
                {
                    Log.Error("Warning! Pattern search for '{0}' has too many words: {2}", wordPattern.Pattern, wordCount);
                }

                // process each word found using the specified word pattern
                ProcessWordsUntilEmpty(wordPattern, driver, db, adminUser, page, node, url);
            }
            else
            {
                // increment pattern
                var childPatterns = wordPattern.GetWordPatternChildren();

                // recursively process children patterns
                foreach (var childPattern in childPatterns)
                {
                    if (hasMissedLastWord) return;

                    ReadWordsByWordPermutationsRecursive(driver, childPattern, db, adminUser);
                }
            }
        }

        private static IEnumerable<string> GetPermutations(int permutationSize)
        {
            if (permutationSize <= 0) return new string[] { "?" };

            var alphabet = "abcdefghijklmnopqrstuvwxyzøæå";
            var permutations = alphabet.Select(x => x.ToString());

            for (int i = 0; i < permutationSize - 1; i++)
            {
                permutations = permutations.SelectMany(x => alphabet, (x, y) => x + y);
            }

            return permutations;
        }

        private void ReadWordsByWordPermutations(int permutationSize, int letterLength, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            var permutations = GetPermutations(permutationSize);

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
                        // ReadWordsByWordPattern(wordPattern, driver, db, adminUser);
                        ReadWordsByWordPattern(new WordPattern(permutation, letterLength, 1, lastWord), driver, db, adminUser);
                    }
                }
                else
                {
                    // ReadWordsByWordPattern(wordPattern, driver, db, adminUser);
                    ReadWordsByWordPattern(new WordPattern(permutation, letterLength, 1, lastWord), driver, db, adminUser);
                }
            }
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

        private void ReadWordsByWordPattern(WordPattern wordPattern, IWebDriver driver, WordHintDbContext db, User adminUser)
        {
            // get word count
            var (count, documentNode, url, page) = GetWordCountByPattern(driver, wordPattern);

            if (count == 0)
            {
                return;
            }
            else
            {
                Log.Information("Found {0} words when searching for '{1}' on page {2}", count, wordPattern.Pattern, page + 1);
                writer.WriteLine("Found {0} words when searching for '{1}' on page {2}", count, wordPattern.Pattern, page + 1);

                if (count > 108)
                {
                    Log.Error("Warning! Pattern search for '{0}' on page {1} has too many words: {2}", wordPattern.Pattern, page + 1, count);
                }
            }

            ProcessWordsUntilEmpty(wordPattern, driver, db, adminUser, page, documentNode, url);

        }

        private void ProcessWordsUntilEmpty(WordPattern wordPattern, IWebDriver driver, WordHintDbContext db, User adminUser, int page, HtmlNode documentNode, string url)
        {
            while (true)
            {
                Log.Information("Processing pattern search for '{0}' on page {1}", wordPattern.Pattern, page + 1);
                writer.WriteLine("Processing pattern search for '{0}' on page {1}", wordPattern.Pattern, page + 1);

                // parse all words
                var words = ReadWordsAgilityPack(documentNode, adminUser);

                foreach (var word in words)
                {
                    if (wordPattern.IsMatchLastWord)
                    {
                        Log.Information("The current pattern matches the last-word: {0} = {1}. Current word: {2}", wordPattern.Pattern, wordPattern.LastWord, word.Value);
                        hasFoundPattern = true;

                        var wordRemoveDiacriticsToNorwegian = word.Value.RemoveDiacriticsToNorwegian();
                        if (wordRemoveDiacriticsToNorwegian == wordPattern.LastWord)
                        {
                            Log.Information("The current word matches the last-word: {0} = {1}", word.Value, wordPattern.LastWord);
                            hasFoundLastWord = true;
                        }
                    }
                    else
                    {
                        if (hasFoundPattern)
                        {
                            // if the pattern not any longer match, we never found the word - has it been deleted?
                            Log.Error("Warning! The current pattern does not any longer match the last-word: {0} = {1}. Current word: {2}", wordPattern.Pattern, wordPattern.LastWord, word.Value);
                            writer.WriteLine("Warning! The current pattern does not any longer match the last-word: {0} = {1}. Current word: {2}", wordPattern.Pattern, wordPattern.LastWord, word.Value);
                            hasMissedLastWord = true;
                            return;
                        }
                    }

                    if (hasFoundLastWord) GetWordSynonyms(word, driver, db, adminUser);
                }

                // go to next page if exist
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

        private void ProcessSynonymsUntilEmpty(Word word, IWebDriver driver, WordHintDbContext db, User adminUser, int page, HtmlNode documentNode, string url)
        {
            while (true)
            {
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);
                writer.WriteLine("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);

                // parse all related words
                var relatedWords = ReadRelatedWordsAgilityPack(documentNode, adminUser);

                // and add to database
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer);

                // go to next page if exist
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
                var decodedeNextPageUrl = WebUtility.UrlDecode(nextPageUrl).Replace("&amp;", "&");

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

        private IList<Word> ReadRelatedWordsAgilityPack(HtmlNode node, User adminUser)
        {
            // parse all related words
            var tableRows = node.FindNodes(By.XPath("/html/body//div[@class='results']/table/tbody/tr"));

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

        class WordPattern
        {
            int IndentSize = 4;

            public string Permutation { get; set; }
            public int Depth { get; set; }
            public int Length { get; set; }
            public string LastWord { get; set; }

            public string Pattern
            {
                get
                {
                    return Permutation.PadRight(Length, '?');
                }
            }

            public WordPattern(string permutation, int length, int depth, string lastWord)
            {
                this.Permutation = permutation;
                this.Length = length;
                this.Depth = depth;
                this.LastWord = lastWord;
            }

            public override string ToString()
            {
                var indent = new string(' ', (Depth - 1) * IndentSize);
                return string.Format("{0}{1}: {2}", indent, Depth, Pattern);
            }

            public List<WordPattern> GetWordPatternChildren()
            {
                // if current Permutation is na?? 
                // the permutations are:
                // naa
                // nab
                // etc.
                int permutationSize = Depth + 1;
                int letterLength = Length;
                string lastWord = LastWord;

                var childWordPatterns = new List<WordPattern>();

                var permutations = GetPermutations(permutationSize);

                bool hasFoundWord = false;
                foreach (var permutation in permutations)
                {
                    if (permutation.StartsWith(Permutation))
                    {
                        childWordPatterns.Add(new WordPattern(permutation, letterLength, permutationSize, lastWord));
                        hasFoundWord = true;
                    }
                    else
                    {
                        // if we have found the word and it doesn't any longer start with the right letters, we have passed the pattern so break
                        if (hasFoundWord) break;
                    }
                }

                return childWordPatterns;
            }

            public bool IsMatchLastWord
            {
                get
                {
                    return IsMatch(this.LastWord);
                }
            }

            public bool IsMatch(string word)
            {
                if (word == null) return false;

                if (word.Length > Length)
                {
                    return false;
                }
                else if (word.Length < Length)
                {
                    return false;
                }
                else
                {
                    // same length so compare
                    var patternRegexp = Pattern.Replace('?', '.');
                    Match match = Regex.Match(word, patternRegexp, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}