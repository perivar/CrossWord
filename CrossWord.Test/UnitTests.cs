using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CrossWord.Test;

public class CrossTest
{
    [Fact]
    public void CrossBoardPreprocessTest()
    {
        ICrossBoard cb = new CrossBoard(5, 5);
        for (int i = 0; i < 5; i++)
        {
            var sw = new StartWord
            {
                StartX = i,
                StartY = 0
            };
            cb.AddStartWord(sw);
            if (i > 0)
            {
                sw.StartX = 0;
                sw.StartY = i;
                cb.AddStartWord(sw);
            }
            else
            {
                sw.StartX = 2;
                sw.StartY = 2;
                cb.AddStartWord(sw);
            }
        }

        cb.Preprocess(new Dictionary(cb.MaxWordLength));
        Assert.Equal(8, cb.GetPatternCount());
    }

    [Fact]
    public void DictionaryTest()
    {
        ICrossDictionary dict = new Dictionary(4);
        dict.AddWord("duty");
        dict.AddWord("ruty");
        dict.AddWord("suty");
        dict.AddWord("ab");

        int count = dict.GetMatchCount("..ty".ToCharArray());
        Assert.Equal(3, count);

        var al = new List<string>();
        dict.GetMatch("s...".ToCharArray(), al);
        Assert.Single(al);
        Assert.Equal("suty", al[0]);
    }

    [Fact]
    public void FileBoardCreatorTest()
    {
        using var memoryStream = new MemoryStream();
        var w = new StreamWriter(memoryStream);
        w.WriteLine("--   ");
        w.WriteLine("-    ");
        w.WriteLine("     ");
        w.WriteLine("    -");
        w.WriteLine("   --");
        w.Flush();
        memoryStream.Position = 0;
        var board = CrossBoardCreator.CreateFromStreamAsync(memoryStream);
        Assert.True(board != null);
    }
}