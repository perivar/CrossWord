using OpenQA.Selenium;

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
    }
}