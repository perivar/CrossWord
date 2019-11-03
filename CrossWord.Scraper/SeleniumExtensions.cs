using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CrossWord.Scraper.Extensions
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
                // WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                // wait.Until(ExpectedConditions.ElementIsVisible(by));
                driver.WaitUntilVisible(by, timeoutInSeconds);
            }
        }

        // use: element = driver.WaitUntilVisible(By.XPath("//input[@value='Save']"));
        public static IWebElement WaitUntilVisible(
            this IWebDriver driver,
            By by,
            int secondsTimeout = 10)
        {
            var wait = new WebDriverWait(driver, new TimeSpan(0, 0, secondsTimeout));
            var element = wait.Until<IWebElement>((condition) =>
            {
                try
                {
                    var elementToBeDisplayed = driver.FindElement(by);
                    if (elementToBeDisplayed.Displayed)
                    {
                        return elementToBeDisplayed;
                    }
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }

            });
            return element;
        }
    }
}