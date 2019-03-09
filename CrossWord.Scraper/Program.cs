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

            // try
            // {
            //     var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            //     wait.Until(c => c.Url.Equals(""));
            // }
            // catch (WebDriverTimeoutException)
            // {
            //     Console.WriteLine("Timeout - Logged in to AliExpress to late. Stopping.");
            //     return;
            // }

            // go to search list, first page
            var query = "test";
            int page = 0;

            string url = string.Format("{0}?a={1}&b=&p={2}", "https://www.kryssord.org/search.php", query, page);
            driver.Navigate().GoToUrl(url);

            while (true)
            {
                // parse all words
                IWebElement tableElement = driver.FindElement(By.XPath("/html/body//div[@class='results']/table/tbody"));
                IList<IWebElement> tableRow = tableElement.FindElements(By.TagName("tr"));
                IList<IWebElement> rowTD;
                foreach (IWebElement row in tableRow)
                {
                    rowTD = row.FindElements(By.TagName("td"));
                    var w = rowTD[0].Text;
                    Console.WriteLine(w);
                }

                // go to next page if exist
                var selectedPage = driver.FindElement(By.XPath("//div[@class='pages']/ul/li/a/span[@class='mobile-hide']"));
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

        static Tuple<int, int> GetPageNumber(IWebDriver driver)
        {
            int curPage = 0;
            int numPages = 0;
            // var pagesSection = driver.FindElement(By.CssSelector("div[class$='pages']"));
            var selectedPage = driver.FindElement(By.CssSelector("li[class$='active']"));

            // new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(ExpectedConditions.ElementIsVisible(By.XPath("//div[@class='pages']//ul//li[@class='active']//following::li[1]/a"))).Click();

            Regex regexObj = new Regex(@"(\d+)/(\d+)", RegexOptions.IgnoreCase);
            Match matchResults = regexObj.Match(selectedPage.ToString());
            if (matchResults.Success)
            {
                curPage = int.Parse(matchResults.Groups[1].Value);
                numPages = int.Parse(matchResults.Groups[2].Value);
                return new Tuple<int, int>(curPage, numPages);
            }

            return null;
        }

        static bool IsElementPresent(IWebDriver driver, By by)
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
    }
}
