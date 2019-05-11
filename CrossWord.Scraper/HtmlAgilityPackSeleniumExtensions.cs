using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium;

namespace CrossWord.Scraper
{
    public static class HtmlAgilityPackSeleniumExtensions
    {
        // I used the Css2XPath Reloaded library by Jon Humphrey to convert By objects to xpaths
        // https://bitbucket.org/jonrandahl/css2xpath-reloaded/src/master/
        public static string ToCSSQuerySelection(this By by)
        {
            var (byType, byArgs) = by.ToTypeAndArgument();
            return GetCSSQuerySelection(byType, byArgs);
        }

        public static (string, string) ToTypeAndArgument(this By by)
        {
            string[] byTypes = { "By.ClassName[Contains]:", "By.CssSelector: ", "By.Id: ", "By.LinkText: ", "By.Name: ", "By.PartiaILinkText:", "By.TagName: ", "By.XPath: " };
            string byText = by.ToString();
            string byType = byTypes.FirstOrDefault(t => byText.Contains(t));
            string byArgs = byText?.Replace(byType, "");

            return (byType, byArgs);
        }

        public static string GetCSSQuerySelection(string byType, string byArgs)
        {
            string css;
            switch (byType)
            {
                case "By.ClassName[Contains]: ":
                    css = $".{byArgs}";
                    return css;
                case "By.CssSelector: ":
                    return byArgs;
                case "By.Id: ":
                    css = $"#{byArgs}";
                    return css;
                case "By.LinkText: ":
                    return $"descendant-or-self::a[text()='{byArgs}' or .//*[text() = '{byArgs}']]";
                case "By.Name: ":
                    css = $"*[name='{byArgs}']";
                    return css;
                case "By.PartiaILinkText: ":
                    return $"descendant-or-self::a[contains(text(), '{byArgs}') or .//*[contains(text(), '{byArgs}')]]";
                case "By.TagName: ":
                    return byArgs;
                case "By.XPath: ":
                    return byArgs;
                default:
                    throw new NotSupportedException("Unsupported By type.");
            }
        }

        public static bool IsXPath(string byType)
        {
            return (byType == "By.XPath: ");
        }

        public static HtmlNode GetDocumentNode(this IWebDriver driver)
        {
            string html = driver.PageSource;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode;
        }

        public static HtmlNode FindNode(this IWebDriver driver, string xpath)
        {
            HtmlNode doc = driver.GetDocumentNode();
            return doc.SelectSingleNode(xpath);
        }

        public static HtmlNode FindNode(this IWebDriver driver, By by)
        {
            HtmlNode doc = driver.GetDocumentNode();
            var (byType, byArgs) = by.ToTypeAndArgument();
            if (IsXPath(byType))
            {
                return doc.SelectSingleNode(byArgs);
            }
            else
            {
                return doc.QuerySelector(byArgs);
            }
        }

        public static HtmlNode FindNode(this HtmlNode node, string xpath)
        {
            return node.SelectSingleNode(xpath);
        }

        public static HtmlNode FindNode(this HtmlNode node, By by)
        {
            var (byType, byArgs) = by.ToTypeAndArgument();
            if (IsXPath(byType))
            {
                return node.SelectSingleNode(byArgs);
            }
            else
            {
                return node.QuerySelector(byArgs);
            }
        }

        public static IList<HtmlNode> FindNodes(this IWebDriver driver, string xpath)
        {
            HtmlNode doc = driver.GetDocumentNode();
            return doc.SelectNodes(xpath);
        }

        public static IList<HtmlNode> FindNodes(this IWebDriver driver, By by)
        {
            HtmlNode doc = driver.GetDocumentNode();
            var (byType, byArgs) = by.ToTypeAndArgument();
            if (IsXPath(byType))
            {
                return doc.SelectNodes(byArgs);
            }
            else
            {
                return doc.QuerySelectorAll(byArgs);
            }
        }

        public static IList<HtmlNode> FindNodes(this HtmlNode node, string xpath)
        {
            return node.SelectNodes(xpath);
        }


        public static IList<HtmlNode> FindNodes(this HtmlNode node, By by)
        {
            var (byType, byArgs) = by.ToTypeAndArgument();
            if (IsXPath(byType))
            {
                return node.SelectNodes(byArgs);
            }
            else
            {
                return node.QuerySelectorAll(byArgs);
            }
        }
    }
}