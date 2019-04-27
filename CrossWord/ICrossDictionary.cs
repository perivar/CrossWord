using System.Collections.Generic;

namespace CrossWord
{
    public interface ICrossDictionary
    {
        int MaxWordLength { get; }
        IEnumerable<string>[] Words { get; }
        IDictionary<string, string> Descriptions { get; }

        void AddWord(string word);
        int GetWordOfLengthCount(int length);
        int GetMatchCount(char[] pattern);
        void GetMatch(char[] pattern, List<string> matched);

        void AddDescription(string word, string description);
        bool TryGetDescription(string word, out string description);

        /// <summary>
        /// Make sure all the words have descriptions 
        /// </summary>
        /// <param name="words">a list of words</param>
        void AddAllDescriptions(List<string> words);

        void ResetDictionary(int maxWordLength);

    }
}