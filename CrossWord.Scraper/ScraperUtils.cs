using System;
using System.Globalization;
using System.Linq;

namespace CrossWord.Scraper
{
    public class ScraperUtils
    {
        public static int CountNumberOfWords(string text)
        {
            char[] delimiters = new char[] { ' ', '\r', '\n' };
            return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static int CountNumberOfLetters(string text)
        {
            return text.Count(c => c != ' ');
        }

        /// <summary>
        /// Parse the string using format string, if failed return Now
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="formatString"></param>
        /// <returns>date time or Now</returns>
        /// <example>var datetime = ParseDateTimeOrNow(date, "yyyy-MM-dd")</example>
        public static DateTime ParseDateTimeOrNow(string dateString, string formatString)
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

        public static string EscapeUrlString(string value)
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
    }
}