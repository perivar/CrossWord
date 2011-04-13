using System.Collections.Generic;

namespace CrossWord
{
    public interface ICrossDictionary
    {
        void AddWord(string aWord);
        int GetWordOfLengthCount(int aLength);
        int GetMatchCount(char[] aPattern);
        void GetMatch(char[] aPattern, IList<string> aMatched);
    }
}