using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossWord
{
    public class Dictionary : object, ICrossDictionary
    {
        readonly WordFilter _filter;
        readonly IList<string>[] _words; //different array list for each word length
        readonly WordIndex[] _indexes;
        readonly int _maxWordLength;
        readonly Dictionary<string, string> _description;

        public Dictionary(int maxWordLength)
        {
            _maxWordLength = maxWordLength;
            _words = new List<string>[maxWordLength + 1];
            for (int i = 1; i <= maxWordLength; i++)
            {
                _words[i] = new List<string>();
            }
            _indexes = new WordIndex[maxWordLength + 1];
            for (int i = 1; i <= maxWordLength; i++)
            {
                _indexes[i] = new WordIndex(i);
            }
            _filter = new WordFilter(1, maxWordLength);
            _description = new Dictionary<string, string>();
        }

        public Dictionary(string aFileName, int maxWordLength)
            : this(maxWordLength)
        {
            if (Path.GetExtension(aFileName).ToLower().Equals(".json"))
            {
                // read json files
                using (StreamReader r = new StreamReader(aFileName))
                {
                    var json = r.ReadToEnd();
                    var jobj = JObject.Parse(json);
                    foreach (var item in jobj.Properties())
                    {
                        var description = item.Name;
                        var values = item.Values();
                        foreach (var value in values)
                        {
                            var word = value.Value<string>().ToUpper();
                            AddWord(word);
                            AddDescription(word, description);
                        }
                    }
                }
            }
            else
            {
                // read text files
                using (StreamReader reader = File.OpenText(aFileName))
                {
                    string str = reader.ReadLine();
                    TextInfo ti = new CultureInfo("en-US").TextInfo;
                    while (str != null)
                    {
                        int pos = str.IndexOf('|');
                        if (pos == -1)
                        {
                            AddWord(ti.ToUpper(str));
                        }
                        else
                        {
                            var word = ti.ToUpper(str.Substring(0, pos));
                            AddWord(word);
                            AddDescription(word, str.Substring(pos + 1));
                        }
                        str = reader.ReadLine();
                    }
                }
            }
        }

        public int MaxWordLength
        {
            get { return _maxWordLength; }
        }

        public void AddDescription(string word, string description)
        {
            _description[word] = description;
        }

        public bool TryGetDescription(string word, out string description)
        {
            return _description.TryGetValue(word, out description);
        }

        public void AddWord(string aWord)
        {
            if (!_filter.Filter(aWord)) return;
            _indexes[aWord.Length].IndexWord(aWord, _words[aWord.Length].Count);
            _words[aWord.Length].Add(aWord);
        }

        public int GetWordOfLengthCount(int aLength)
        {
            return _words[aLength].Count;
        }

        static bool IsEmptyPattern(char[] aPattern)
        {
            if (aPattern == null) return true;
            foreach (var c in aPattern)
            {
                if (c != '.') return false;
            }
            return true;
        }

        public int GetMatchCount(char[] aPattern)
        {
            if (IsEmptyPattern(aPattern))
                return _words[aPattern.Length].Count;
            var indexes = _indexes[aPattern.Length].GetMatchingIndexes(aPattern);
            return indexes != null ? indexes.Count : 0;
        }

        public void GetMatch(char[] aPattern, List<string> matched)
        {
            if (IsEmptyPattern(aPattern))
            {
                matched.AddRange(_words[aPattern.Length]);
                return;
            }
            var indexes = _indexes[aPattern.Length].GetMatchingIndexes(aPattern);
            if (indexes == null) return;
            foreach (var idx in indexes)
            {
                matched.Add(_words[aPattern.Length][idx]);
            }
        }
    }
}