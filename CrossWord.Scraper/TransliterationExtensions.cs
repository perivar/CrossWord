using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrossWord.Scraper.Extensions
{
    public static class TransliterationExtensions
    {
        private static Dictionary<char, string> charmap = new Dictionary<char, string>() {
            {'À', "A"}, {'Á', "A"}, {'Â', "A"}, {'Ã', "A"}, {'Ä', "Ae"}, {'Å', "A"}, {'Æ', "Ae"},
            {'Ç', "C"},
            {'È', "E"}, {'É', "E"}, {'Ê', "E"}, {'Ë', "E"},
            {'Ì', "I"}, {'Í', "I"}, {'Î', "I"}, {'Ï', "I"},
            {'Ð', "Dh"}, {'Þ', "Th"},
            {'Ñ', "N"},
            {'Ò', "O"}, {'Ó', "O"}, {'Ô', "O"}, {'Õ', "O"}, {'Ö', "Oe"}, {'Ø', "Oe"},
            {'Ù', "U"}, {'Ú', "U"}, {'Û', "U"}, {'Ü', "Ue"},
            {'Ý', "Y"},
            {'ß', "ss"},
            {'à', "a"}, {'á', "a"}, {'â', "a"}, {'ã', "a"}, {'ä', "ae"}, {'å', "a"}, {'æ', "ae"},
            {'ç', "c"},
            {'è', "e"}, {'é', "e"}, {'ê', "e"}, {'ë', "e"},
            {'ì', "i"}, {'í', "i"}, {'î', "i"}, {'ï', "i"},
            {'ð', "dh"}, {'þ', "th"},
            {'ñ', "n"},
            {'ò', "o"}, {'ó', "o"}, {'ô', "o"}, {'õ', "o"}, {'ö', "oe"}, {'ø', "oe"},
            {'ù', "u"}, {'ú', "u"}, {'û', "u"}, {'ü', "ue"},
            {'ý', "y"}, {'ÿ', "y"}
        };

        private static Dictionary<char, string> charmapNorwegian = new Dictionary<char, string>() {
            {'À', "A"}, {'Á', "A"}, {'Â', "A"}, {'Ã', "A"}, {'Ä', "Æ"},
            {'Ç', "C"},
            {'È', "E"}, {'É', "E"}, {'Ê', "E"}, {'Ë', "E"},
            {'Ì', "I"}, {'Í', "I"}, {'Î', "I"}, {'Ï', "I"},
            {'Ð', "Dh"}, {'Þ', "Th"},
            {'Ñ', "N"},
            {'Ò', "O"}, {'Ó', "O"}, {'Ô', "O"}, {'Õ', "O"}, {'Ö', "Ø"},
            {'Ù', "U"}, {'Ú', "U"}, {'Û', "U"}, {'Ü', "Y"},
            {'Ý', "Y"},
            {'ß', "ss"},
            {'à', "a"}, {'á', "a"}, {'â', "a"}, {'ã', "a"}, {'ä', "æ"},
            {'ç', "c"},
            {'è', "e"}, {'é', "e"}, {'ê', "e"}, {'ë', "e"},
            {'ì', "i"}, {'í', "i"}, {'î', "i"}, {'ï', "i"},
            {'ð', "dh"}, {'þ', "th"},
            {'ñ', "n"},
            {'ò', "o"}, {'ó', "o"}, {'ô', "o"}, {'õ', "o"}, {'ö', "ø"},
            {'ù', "u"}, {'ú', "u"}, {'û', "u"}, {'ü', "y"},
            {'ý', "y"}, {'ÿ', "y"}
        };

        public static string RemoveDiacriticsToNorwegian(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return text.Aggregate(
                          new StringBuilder(),
                          (sb, c) =>
                          {
                              string r;
                              if (charmapNorwegian.TryGetValue(c, out r))
                              {
                                  return sb.Append(r);
                              }
                              return sb.Append(c);
                          }).ToString();
        }

        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return text.Aggregate(
                          new StringBuilder(),
                          (sb, c) =>
                          {
                              string r;
                              if (charmap.TryGetValue(c, out r))
                              {
                                  return sb.Append(r);
                              }
                              return sb.Append(c);
                          }).ToString();
        }

    }
}