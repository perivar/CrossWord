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

        public KryssordScraper(string connectionString, string signalRHubURL, string siteUsername, string sitePassword, int startLetterCount, int endLetterCount, bool doContinueWithLastWord, bool isScraperSwarm)
        {
            this.connectionString = connectionString;
            this.signalRHubURL = signalRHubURL;
            this.source = "kryssord.org";

            // set writer identifier as pattern            
            this.writer = new SignalRClientWriter(signalRHubURL, startLetterCount.ToString());
            writer.WriteLine("Starting {0} Scraper ....", this.source);

            // make sure that no chrome and chrome drivers are running
            // cannot do this here, since several instances of the scraper might be running in parallel
            // do this before this class is called instead
            // KillAllChromeDriverInstances();

            DoScrape(siteUsername, sitePassword, startLetterCount, endLetterCount, source, doContinueWithLastWord, isScraperSwarm);
        }

        private void DoScrape(string siteUsername, string sitePassword, int startLetterCount, int endLetterCount, string source, bool doContinueWithLastWord, bool isScraperSwarm)
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

#if DEBUG
                // some patterns give back a word with one less character than asked for - it seems the Ø is messing their system up
                // UTF8 two byte problem?
                // TROND?K?????         gives TROND KJØLL
                // VEBJØRN?B????        gives VEBJØRN BERG
                // WILLY?R????????      gives WILLY RØGEBERG
                // THORBJØRN?H???????   gives THORBJØRN HÅRSTAD

                // lastWordString = "TRONSMOS VEG"; // word before TROND KJØLL
                // letterCount = 12;

                // lastWordString = "ÅSTED FOR DRAMAET ROMEO OG JULIE";
                // letterCount = 32;

                // lastWordString = "GUTTENAVN PÅ \"A\"";
                // letterCount = 16;
                // endLetterCount = 17;

                // lastWordString = "TALL SOM ANGIR FORHOLDET MELLOM ET LEGEMES HASTIGHET OG LYDENS";
                // lastWordString = "ÅPNINGSKONSERTSTYKKE";
                // letterCount = lastWordString.Length;
                // endLetterCount = 300;
#endif


                using (var driver = ChromeDriverUtils.GetChromeDriver(true))
                {
                    DoLogon(driver, siteUsername, sitePassword);

                    for (int i = startLetterCount; i < endLetterCount; i++)
                    {
                        // reset global variables
                        hasFoundPattern = false; // this is the first stage, we match the pattern
                        hasFoundLastWord = false; // this is the second stage, we not only match the pattern but the word as well
                        hasMissedLastWord = false;

                        string lastWordString = null;
                        if (doContinueWithLastWord)
                        {
                            lastWordString = WordDatabaseService.GetLastWordFromLetterCount(db, source, i);
                        }

                        // don't skip any words when the last word is empty
                        if (lastWordString == null)
                        {
                            hasFoundLastWord = true;
                        }

                        // added break to support several docker instances scraping in swarms
                        if (isScraperSwarm && (i > startLetterCount))
                        {
                            Log.Error("Warning! Quitting since the current letter length > letter count: {0} / {1}", i, startLetterCount);
                            break;
                        }

                        ReadWordsByWordPermutations(i, driver, db, adminUser, lastWordString);
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

        private Tuple<int, HtmlNode, string, int> GetWordCountByPattern(IWebDriver driver, WordPattern wordPattern)
        {
            // go to search result page            
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern.Pattern, page);

            var (count, node) = GetWordCountByWordPattern(driver, url);
            return new Tuple<int, HtmlNode, string, int>(count, node, url, page);
        }

        private void ReadWordsByWordPermutations(int letterLength, IWebDriver driver, WordHintDbContext db, User adminUser, string lastWord)
        {
            int wordLength = letterLength;

            int depth = 1;

            // if we are using only one letter, set the depth to zero to force the pattern to become ?
            if (letterLength == 1)
            {
                depth = 0;
            }

            // set wordlength based on last word if we are not processing in swarm mode and are continuing
            // to read patterns with increasing length
            // this assumes that we found the last word for the first pattern match
            if (!hasFoundLastWord && lastWord != null)
            {
                wordLength = lastWord.Length;

                // disabling this as it doesn't give any real benefits - only misses words that start with one character for high letter counts
                // if the word is longer than 3 characters, use the two first letters as pattern
                // if (wordLength > 2)
                // {
                //     depth = 2;
                // }
            }

            var permutations = GetPermutations(depth);


            bool hasFound = false;
            foreach (var permutation in permutations)
            {
                if (hasMissedLastWord) return;

                // skip until we reach last word beginning
                if (!hasFoundLastWord && lastWord != null)
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
            // but maximum 4 levels
            if (wordCount <= 108 || wordPattern.Depth > 3)
            {
                if (wordPattern.Depth > 3)
                {
                    Log.Error("Warning! Pattern search depth is now {0}. Found {1} words when searching for '{2}' on page {3}", wordPattern.Depth, wordCount, wordPattern.Pattern, page + 1);
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

                    // if we have a last word - make sure to skip until the pattern matches
                    if (!hasFoundLastWord && childPattern.LastWord != null && !childPattern.IsMatchLastWord)
                    {
                        // skip pattern
                        Log.Information("Skipping pattern '{0}'.", childPattern.Pattern);
                    }
                    else
                    {
                        Log.Information("Processing pattern '{0}'.", childPattern.Pattern);
                        ReadWordsByWordPermutationsRecursive(driver, childPattern, db, adminUser);
                    }
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

                        // we might have had to add question marks at the end of the string to fix the length bug at the site                    
                        if (wordRemoveDiacriticsToNorwegian == wordPattern.LastWord.TrimEnd('?'))
                        {
                            Log.Information("The current word matches the last-word: {0} = {1}", word.Value, wordPattern.LastWord);
                            hasFoundLastWord = true;
                        }
                    }
                    else
                    {
                        if (!hasFoundLastWord && hasFoundPattern)
                        {
                            // if the pattern not any longer match, we never found the word - has it been deleted?
                            Log.Error("Warning! The current pattern does not any longer match the last-word: {0} = {1}. Current word: {2}", wordPattern.Pattern, wordPattern.LastWord, word.Value);
                            writer.WriteLine("Warning! The current pattern does not any longer match the last-word: {0} = {1}. Current word: {2}", wordPattern.Pattern, wordPattern.LastWord, word.Value);
                            hasMissedLastWord = true;
                            return;
                        }
                    }

                    if (hasFoundLastWord)
                    {
                        string currentValue = word.Value;

                        // check if this is one of the buggy words from their site where the words found don't have the same length as the pattern says it should have
                        if (wordPattern.Length != word.Value.Length)
                        {
                            Log.Error("Warning! The current word doesn't match the length of the query pattern: {0} = {1}", word.Value, wordPattern.Pattern);
                            writer.WriteLine("Warning! The current word doesn't match the length of the query pattern: {0} = {1}", word.Value, wordPattern.Pattern);

                            if (wordPattern.Length > word.Value.Length)
                            {
                                currentValue = currentValue + new string('?', wordPattern.Length - word.Value.Length);
                            }
                            else
                            {
                                currentValue = currentValue.Substring(0, wordPattern.Length);
                            }
                        }
                        else
                        {
                            // everything is OK
                        }

                        // update that we are processing this word, ignore length and comment
                        WordDatabaseService.UpdateState(db, source, new Word() { Value = currentValue.ToUpper(), Source = source, CreatedDate = DateTime.Now }, writer);

                        GetWordSynonyms(word, driver, db, adminUser);
                    }
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
                // we are updating the state earlier, so make sure we don't do that here also
                WordDatabaseService.AddToDatabase(db, this.source, word, relatedWords, writer, false);

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