using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace CrossWord.Scraper.Extensions
{
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Read a configuration key as int
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="key">key</param>
        /// <param name="defaultValue">default value to return if the key is null or missing</param>
        /// <returns>the int value or the default value</returns>
        public static int GetIntValue(this IConfiguration configuration, string key, int defaultValue)
        {
            string stringValue = configuration[key] ?? defaultValue.ToString();
            _ = int.TryParse(stringValue, out int returnValue);
            return returnValue;
        }

        /// <summary>
        /// Read a configuration key as boolean
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="key">key</param>
        /// <param name="defaultValue">default value to return if the key is null or missing</param>
        /// <returns>the boolean value or the default value</returns>
        public static bool GetBoolValue(this IConfiguration configuration, string key, bool defaultValue)
        {
            string stringValue = configuration[key] ?? defaultValue.ToString();
            _ = bool.TryParse(stringValue, out bool returnValue);
            return returnValue;
        }

        /// <summary>
        /// Read and parse a comma separated array variable wrapped in characters like ' and "
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="key">key</param>
        /// <param name="defaultValue">default value to return if the key is null or missing</param>
        /// <returns>the key value as a list or empty list</returns>
        /// <example>docker-compose: - KNOWNPROXIES='10.0.0.1, 10.0.0.2'</example>
        public static List<string> GetArrayValues(this IConfiguration configuration, string key, string defaultValue)
        {
            string stringValue = configuration[key] ?? defaultValue;
            var stringValues = stringValue
                .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(proxy => proxy.Trim(' ', '\t', '\'', '"')).Where(s => s != string.Empty).ToList();

            return stringValues;
        }

        /// <summary>
        /// Very similar to GetArrayValues() but uses the more modern GetSection to parse an array directly
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="key">key</param>
        /// <param name="defaultList">default value to return if the key is null or missing</param>
        /// <returns>the key value as a list or empty list</returns>
        /// <example>appconfig.json: "Words": [ "Word1", "Word2", "Word3" ]</example>
        /// <example>docker-compose: - Words__0=${Words__0}\n- Words__1=${Words__1}</example>
        public static List<string> GetSectionList(this IConfiguration configuration, string key, List<string> defaultList)
        {
            var stringArray = configuration.GetSection(key).Get<List<string>>();
            if (stringArray != null)
            {
                return stringArray;
            }
            return defaultList;
        }
    }
}