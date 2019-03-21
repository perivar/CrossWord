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
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using Serilog.Events;

namespace CrossWord.Scraper
{
    public class Program
    {
        const string DEFAULT_LOG_PATH = "crossword_scraper.log";
        const string DEFAULT_ERROR_LOG_PATH = "crossword_scraper_error.log";

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                        .AddCommandLine(args)
                        .AddEnvironmentVariables()
                        .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                // .MinimumLevel.Debug() // enable ef core logging
                // .MinimumLevel.Information() // disable ef core logging
                // .WriteTo.File(DEFAULT_LOG_PATH)
                // .WriteTo.Console()
                // .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                // .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(DEFAULT_ERROR_LOG_PATH))
                .CreateLogger();

            Log.Error("Starting CrossWord.Scraper ....");

            // start DOCKER on port 3360
            // docker run -p 3360:3306 --name mysqldb -e MYSQL_ROOT_PASSWORD=password -d mysql:8.0.15            

            // Build database connection string
            var dbhost = configuration["DBHOST"] ?? "localhost";
            var dbport = configuration["DBPORT"] ?? "3306";
            var dbuser = configuration["DBUSER"] ?? "user";
            var dbpassword = configuration["DBPASSWORD"] ?? "password";
            var database = configuration["DATABASE"] ?? "dictionary";

            string connectionString = $"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={database}; charset=utf8;";

            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
                // setup database
                // db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                string siteUsername = configuration["kryssord.org:Username"];
                string sitePassword = configuration["kryssord.org:Password"];

                KillAllChromeDriverInstances();

                // string userDataDir = @"C:\Users\perner\AppData\Local\Google\Chrome\User Data\Default";
                // string userDataArgument = string.Format("--user-data-dir={0}", userDataDir);

                var outPutDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var chromeDriverPath = outPutDirectory;
                string driverExecutableFileName = null;

                ChromeOptions options = new ChromeOptions();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    chromeDriverPath = "/usr/local/bin/";
                    driverExecutableFileName = "chromedriver";

                    // options.AddArguments("--disable-gpu"); // seem to be needed on ubuntu
                    options.AddArguments("--headless");
                    options.AddArguments("--no-sandbox");
                    options.AddArguments("--whitelisted-ips='127.0.0.1'");
                    options.AddArguments("--disable-extensions");

                    //options.AddArguments(userDataArgument);
                    //options.AddArguments("--start-maximized");
                    //options.AddArgument("--log-level=3");
                    //options.AddArguments("--ignore-certificate-errors");
                    //options.AddArguments("--ignore-ssl-errors");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    driverExecutableFileName = "chromedriver.exe";
                    options.AddArguments("window-size=1920,1080");
                }

                ChromeDriverService service = ChromeDriverService.CreateDefaultService(chromeDriverPath, driverExecutableFileName);
                service.Port = 9515;
                service.WhitelistedIPAddresses = "127.0.0.1";
                // service.EnableVerboseLogging = true;

                IWebDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(30));
                // driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(40);
                // driver.Manage().Window.Maximize();

                Log.Information("Using chromedriver path: '{0}', options: {1}", chromeDriverPath, options);

                // IWebDriver driver = new ChromeDriver(chromeDriverPath, options);
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

                // set admin user
                var user = new User()
                {
                    FirstName = "Admin",
                    LastName = "Admin",
                    UserName = "",
                    Password = "",
                    isVIP = true
                };
                // db.Users.Add(user);
                // db.SaveChanges();

                // Testing
                // ReadWordsByWordPattern("RV", driver, db, user);

                // find out how far we have got in the processing
                var lastWord = db.Words.OrderByDescending(p => p.WordId).FirstOrDefault();
                var lastHint = db.Hints.OrderByDescending(p => p.HintId).FirstOrDefault();

                // read all one letter words
                if (lastWord == null || lastWord != null && lastWord.Value.Length < 2)
                {
                    ReadWordsByWordPattern("1", driver, db, user, lastWord, lastHint);
                }

                // read 2 and more letter words
                for (int i = 2; i < 200; i++)
                {
                    ReadWordsByWordPermutations(2, i, driver, db, user, lastWord, lastHint);
                }

                // make sure to clean the chrome driver from memory
                driver.Close();
                driver.Quit();
                driver = null;
            }
        }

        static void KillAllChromeDriverInstances()
        {
            System.Diagnostics.ProcessStartInfo p;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                p = new ProcessStartInfo("cmd.exe", "/C " + "taskkill /f /im chromedriver.exe");
                Log.Information("Killing Chromedriver on Windows: '{0}'", "cmd.exe " + "/C " + "taskkill /f /im chromedriver.exe");
                proc.StartInfo = p;
                proc.Start();
                proc.WaitForExit();
                proc.Close();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // p = new ProcessStartInfo("/bin/sh", @"kill $(ps aux | grep 'chromedrive[r]' | awk '{print $2}')");
                // Log.Information("Killing Chromedriver on Linux: '{0}'", "/bin/sh " + "-c " + @"kill $(ps aux | grep 'chromedrive[r]' | awk '{print $2}')");
                // proc.StartInfo = p;
                // proc.Start();
                // proc.WaitForExit();
                // proc.Close();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

            }
        }

        static void ReadWordsByWordPermutations(int permutationSize, int letterLength, IWebDriver driver, WordHintDbContext db, User user, Word lastWord, Hint lastHint)
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
                        if (lastWord.Value.Length > wordPattern.Length)
                        {
                            // skip until same length
                            continue;
                        }
                        else if (lastWord.Value.Length < wordPattern.Length)
                        {
                            // this means we are continuing with longer patterns
                            // and should process as normal
                            hasFoundWord = true;
                        }
                        else
                        {
                            // same length so compare
                            var patternRegexp = wordPattern.Replace('?', '.');
                            Match match = Regex.Match(lastWord.Value, patternRegexp, RegexOptions.IgnoreCase);
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
                        ReadWordsByWordPattern(wordPattern, driver, db, user, lastWord, lastHint);
                    }
                }
                else
                {
                    ReadWordsByWordPattern(wordPattern, driver, db, user, lastWord, lastHint);
                }
            }
        }

        static void ReadWordsByWordPattern(string wordPattern, IWebDriver driver, WordHintDbContext db, User user, Word lastWord, Hint lastHint)
        {
            // go to search result page            
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, wordPattern, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                Log.Information("Processing pattern search for '{0}' on page {1}", wordPattern, page + 1);

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
                        if (n > 108) Log.Error("Warning! Pattern search for '{0}' on page {1} has too many words: {2}", wordPattern, page + 1, n);
                    }
                }

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var wordText = rowTD[0].Text;
                    var userId = rowTD[3].Text;
                    var date = rowTD[4].Text;

                    // check if user already exists
                    User wordUser = null;
                    var existingUser = db.Users.Where(o => o.ExternalId == userId).FirstOrDefault();
                    if (existingUser != null)
                    {
                        wordUser = existingUser;
                    }
                    else
                    {
                        wordUser = new User()
                        {
                            ExternalId = userId
                        };
                    }

                    var word = new Word
                    {
                        Language = "no",
                        Value = wordText,
                        NumberOfLetters = wordText.Count(c => c != ' '),
                        NumberOfWords = CountNumberOfWords(wordText),
                        User = wordUser,
                        CreatedDate = ParseDateTimeOrNow(date, "yyyy-MM-dd")
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

                    GetWordSynonyms(word, driver, db, user, lastHint);
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

        static void GetWordSynonyms(Word word, IWebDriver driver, WordHintDbContext db, User user, Hint lastHint)
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
                Log.Information("Processing synonym search for '{0}' on page {1}", word.Value, page + 1);

                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

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
                        Log.Information("Found {0} synonyms when searching for '{1}' on page {2}", n, word.Value, page + 1);
                        if (n > 108) Log.Error("Warning! synonym search for '{0}' on page {1} has too many words: {2}", word.Value, page + 1, n);
                    }
                }

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var hintText = rowTD[0].Text;
                    var userId = rowTD[3].Text;
                    var date = rowTD[4].Text;

                    // check if user already exists
                    User hintUser = null;
                    var existingUser = db.Users.Where(o => o.ExternalId == userId).FirstOrDefault();
                    if (existingUser != null)
                    {
                        hintUser = existingUser;
                    }
                    else
                    {
                        hintUser = new User()
                        {
                            ExternalId = userId
                        };
                    }

                    var hint = new Hint
                    {
                        Language = "no",
                        Value = hintText,
                        NumberOfLetters = hintText.Count(c => c != ' '),
                        NumberOfWords = CountNumberOfWords(hintText),
                        User = hintUser,
                        CreatedDate = ParseDateTimeOrNow(date, "yyyy-MM-dd")
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
                    }
                    else
                    {
                        Log.Debug("Skipped adding '{0}' as a hint for '{1}' ...", hintText, word.Value);
                    }
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

        private static DateTime ParseDateTimeOrNow(string dateString, string formatString)
        {
            try
            {
                DateTime parsedDateTime = DateTime.ParseExact(dateString,
                                                        formatString,
                                                        CultureInfo.InvariantCulture,
                                                        DateTimeStyles.None);

                return parsedDateTime;
            }
            catch (FormatException)
            {
            }

            return DateTime.Now;
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

        public static int CountNumberOfWords(string text)
        {
            char[] delimiters = new char[] { ' ', '\r', '\n' };
            return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
