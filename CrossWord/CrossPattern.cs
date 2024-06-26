using System;
using System.Linq;

namespace CrossWord;

public class CrossPattern
{
    readonly bool _isHorizontal;
    readonly int _length;
    readonly int _startX;
    readonly int _startY;

    public CrossPattern(int startX, int startY, int length, bool isHorizontal)
    {
        _startX = startX;
        _startY = startY;
        _length = length;
        AdjacentPatterns = new CrossPattern[_length];
        _isHorizontal = isHorizontal;
        Pattern = Enumerable.Repeat('.', length).ToArray();
    }

    public static CrossPattern Empty { get; } = new CrossPattern(0, 0, 0, false);

    public bool IsHorizontal => _isHorizontal;

    public int StartX => _startX;

    public int StartY => _startY;

    public int Length => _length;

    public bool IsPuzzle { get; set; }

    public char[] Pattern { get; set; }

    public int InstantiationCount { get; set; }

    public CrossPattern?[] AdjacentPatterns { get; }

    public CrossTransformation? TryFillPuzzle(ReadOnlySpan<char> word, ICrossDictionary dict)
    {
        for (int i = 0; i < word.Length; i++)
            if (Pattern[i] != '.')
                return null;
        return TryFill("", word, dict, true);
    }

    public CrossTransformation? TryFill(string dictWord, ReadOnlySpan<char> word, ICrossDictionary dict)
    {
        return TryFill(dictWord, word, dict, false);
    }

    int CalcAdjacentIndex(CrossPattern other)
    {
        if (_isHorizontal)
            return _startY - other.StartY;
        return _startX - other.StartX;
    }

    CrossTransformation? TryFill(string dictWord, ReadOnlySpan<char> word, ICrossDictionary dict, bool puzzle)
    {
        var trans = new CrossTransformation(dictWord);
        int instSum = 0;
        for (int i = 0; i < word.Length; i++)
        {
            if (Pattern[i] != '.') continue;

            var adjacentPattern = AdjacentPatterns[i];
            if (adjacentPattern != null)
            {
                int adjIndex = CalcAdjacentIndex(adjacentPattern);
                char c = adjacentPattern.Pattern[adjIndex];
                if (c == '.')
                {
                    char[] adjacent = adjacentPattern.Pattern;
                    adjacent[adjIndex] = word[i];
                    int newInstCount = dict.GetMatchCount(adjacent);
                    adjacent[adjIndex] = '.';
                    if (newInstCount == 0)
                        return null;
                    instSum += newInstCount;
                    trans.AddChangeInstantiation(i, adjacentPattern.InstantiationCount, newInstCount);
                    trans.AddChange(i, adjIndex, word[i]);
                }
                else if (puzzle || c != word[i])
                {
                    return null;
                }
            }

            trans.AddChange(-1, i, word[i]);
        }

        trans.AddChangeInstantiation(-1, InstantiationCount, (int)Constants.Unbounded);
        trans.SumInst = instSum;
        return trans;
    }

    public override string ToString()
    {
        return (_isHorizontal ? "-" : "|") + $",{_startX},{_startY}," + new string(Pattern);
    }

    public object Clone()
    {
        var result = new CrossPattern(_startX, _startY, _length, _isHorizontal)
        {
            InstantiationCount = InstantiationCount,
            IsPuzzle = IsPuzzle,
            Pattern = new char[Pattern.Length]
        };
        Array.Copy(Pattern, result.Pattern, Pattern.Length);
        return result;
    }

    public string GetWord()
    {
        return new string(Pattern);
    }
}