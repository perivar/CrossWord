using System;
using System.Collections.Generic;

namespace CrossWord;

public interface ICrossDictionary
{
    void AddWord(string word);
    int GetWordOfLengthCount(int length);
    int GetMatchCount(ReadOnlySpan<char> pattern);
    void GetMatch(ReadOnlySpan<char> pattern, List<string> matched);
    bool TryGetDescription(string word, out string? description);

    /// <summary>
    /// Make sure all the words have descriptions 
    /// </summary>
    /// <param name="words">a list of words</param>
    void AddAllDescriptions(List<string> words);

    void ResetDictionary(int maxWordLength);
}