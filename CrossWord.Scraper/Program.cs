using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CrossWord.Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            string siteUsername = "kongolav";
            string sitePassword = "kongolav";

            var outPutDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var relativePath = @"..\..\..\";
            var chromeDriverPath = Path.GetFullPath(Path.Combine(outPutDirectory, relativePath));

            string userDataDir = @"C:\Users\perner\AppData\Local\Google\Chrome\User Data\Default";
            string userDataArgument = string.Format("--user-data-dir={0}", userDataDir);

            ChromeOptions options = new ChromeOptions();
            // options.AddArguments(userDataArgument);
            options.AddArguments("--start-maximized");
            options.AddArgument("--log-level=3");
            //options.AddArguments("--ignore-certificate-errors");
            //options.AddArguments("--ignore-ssl-errors");
            IWebDriver driver = new ChromeDriver(chromeDriverPath, options);
            driver.Navigate().GoToUrl("https://www.kryssord.org/login.php");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            var ready = wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            // login if login form is present
            if (IsElementPresent(driver, By.XPath("//input[@name='username']"))
                && IsElementPresent(driver, By.XPath("//input[@name='password']")))
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

            // go to search result page
            var letterCount = 2;
            var query = "";
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, letterCount, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var word = rowTD[0];

                    Console.WriteLine(word);

                    GetWordSynonyms(word.Text, driver);
                }

                // go to next page if exist
                var selectedPage = FindElementOrNull(driver, By.XPath("//div[@class='pages']/ul/li/a/span[contains(., 'Neste')]"));
                if (selectedPage != null)
                {
                    selectedPage.Click();
                }
                else
                {
                    break;
                }
            }
        }

        static void GetWordSynonyms(string word, IWebDriver driver)
        {
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
            var letterCount = "";
            var query = word;
            int page = 0;
            string url = string.Format("{0}?a={1}&b={2}&p={3}", "https://www.kryssord.org/search.php", query, letterCount, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                // parse total number of words found
                var wordCount = driver.FindElement(By.XPath("/html/body//div[@id='content']/h1/strong")).Text;

                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var synonym = rowTD[0].Text;

                    Console.WriteLine("{0} is a synonym for {1}", synonym, word);
                }

                // go to next page if exist
                var selectedPage = FindElementOrNull(driver, By.XPath("//div[@class='pages']/ul/li/a/span[contains(., 'Neste')]"));
                if (selectedPage != null)
                {
                    var nextAnchor = GetParent(selectedPage).GetAttribute("href");
                    driver.Navigate().GoToUrl(nextAnchor);
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

        private static bool IsElementPresent(IWebDriver driver, By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private static IWebElement FindElementOrNull(IWebDriver driver, By by)
        {
            try
            {
                var webElement = driver.FindElement(by);
                return webElement;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        private static IWebElement GetParent(IWebElement node)
        {
            return node.FindElement(By.XPath(".."));
        }
    }
}
