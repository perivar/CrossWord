using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CrossWord.Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new SynonymDbContext())
            {
                // setup database
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                string siteUsername = "kongolav";
                string sitePassword = "kongolav";

                // string userDataDir = @"C:\Users\perner\AppData\Local\Google\Chrome\User Data\Default";
                // string userDataArgument = string.Format("--user-data-dir={0}", userDataDir);

                var outPutDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                // var relativePath = @"..\..\..\";
                // var chromeDriverPath = Path.GetFullPath(Path.Combine(outPutDirectory, relativePath));
                var chromeDriverPath = outPutDirectory;

                ChromeOptions options = new ChromeOptions();
                // options.AddArguments(userDataArgument);
                options.AddArguments("--start-maximized");
                // options.AddArgument("--log-level=3");
                //options.AddArguments("--ignore-certificate-errors");
                //options.AddArguments("--ignore-ssl-errors");
                IWebDriver driver = new ChromeDriver(chromeDriverPath, options);
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

                try
                {
                    // read all one letter words
                    // ReadWordsByWordPattern("1", driver, db);

                    // read 2 and more letter words
                    for (int i = 2; i < 200; i++)
                    {
                        ReadWordsByWordPermutations(2, i, driver, db);
                        // ReadWordsByWordLength(i, driver, db);
                    }
                }
                finally
                {
                    driver.Close();
                    driver.Quit();
                }
            }
        }

        static void ReadWordsByWordPermutations(int permutationSize, int letterLength, IWebDriver driver, SynonymDbContext db)
        {
            var alphabet = "abcdefghijklmnopqrstuvwxyzøæå";
            var permutations = alphabet.Select(x => x.ToString());

            for (int i = 0; i < permutationSize - 1; i++)
            {
                permutations = permutations.SelectMany(x => alphabet, (x, y) => x + y);
            }

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
                ReadWordsByWordPattern(wordPattern, driver, db);
            }
        }

        static void ReadWordsByWordPattern(string wordPattern, IWebDriver driver, SynonymDbContext db)
        {
            // go to search result page            
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                Console.WriteLine("[Processing pattern search for '{0}' page {1}]", wordPattern, page + 1);

                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

                // return if nothing was found
                if (wordCount == "0") return;

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var wordText = rowTD[0].Text;

                    var word = new Word
                    {
                        Language = "no",
                        Value = wordText,
                        NumberOfLetters = wordText.Count(c => c != ' '),
                        NumberOfWords = CountNumberOfWords(wordText),
                        UserId = 1,
                        CreatedDate = DateTime.Now,
                    };

                    db.Words.Add(word);
                    db.SaveChanges();

                    GetWordSynonyms(word, driver, db);
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

        static void GetWordSynonyms(Word word, IWebDriver driver, SynonymDbContext db)
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
            var query = EscapeUrlString(word.Value);
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                Console.WriteLine("[Processing synonym search for {0} page {1}]", word.Value, page + 1);

                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var synonymText = rowTD[0].Text;

                    var synonym = new Word
                    {
                        Language = "no",
                        Value = synonymText,
                        NumberOfLetters = synonymText.Count(c => c != ' '),
                        NumberOfWords = CountNumberOfWords(synonymText),
                        UserId = 1,
                        CreatedDate = DateTime.Now,
                        ParentWordId = word.WordId
                    };

                    db.Words.Add(synonym);
                    db.SaveChanges();

                    Console.WriteLine("Added {0} as a synonym for {1}", synonymText, word.Value);
                }

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

        private static string EscapeUrlString(string value)
        {

            var isNumeric = int.TryParse(value, out int n);
            if (!isNumeric)
            {
                return Uri.EscapeDataString(value);
            }
            else
            {
                return value;
            }
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

        private static int CountNumberOfWords(string text)
        {
            char[] delimiters = new char[] { ' ', '\r', '\n' };
            return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
