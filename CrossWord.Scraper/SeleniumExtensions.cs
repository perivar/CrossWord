using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CrossWord.Scraper
{
    public static class SeleniumExtensions
    {
        public static bool IsElementPresent(this IWebDriver driver, By by)
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

        public static IWebElement FindElementOrNull(this IWebDriver driver, By by)
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

        public static IWebElement GetParent(this IWebElement node)
        {
            return node.FindElement(By.XPath(".."));
        }

        // example WaitForElementLoad(By.CssSelector("div#div1 div strong a"), 10);
        public static void WaitForElementLoad(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                wait.Until(ExpectedConditions.ElementIsVisible(by));
            }
        }
    }
}