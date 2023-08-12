using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CrossWord.Models;

namespace CrossWord;

public static class CrossBoardCreator
{
    public static async Task<ICrossBoard> CreateFromUrlAsync(string url)
    {
        var model = await GetCrossWordModelFromUrlAsync(url);
        var board = model.ToCrossBoard();

        return board;
    }

    private static string RandomDateString(DateTime startDate, DateTime endDate)
    {
        TimeSpan timeSpan = endDate - startDate;
        var randomTest = new Random();
        TimeSpan newSpan = new(0, randomTest.Next(0, (int)timeSpan.TotalMinutes), 0);
        DateTime newDate = startDate + newSpan;

        return string.Format(CultureInfo.InvariantCulture, "{0:yyyy/MM/dd}", newDate);
    }

    public static async Task<CrossWordTimes> GetCrossWordModelFromUrlAsync(string url)
    {
        using var httpClient = new HttpClient();

        CrossWordTimes? model;
        if (url.ToLower().Equals("http-random"))
        {
            var start = new DateTime(1976, 01, 01);
            var end = new DateTime(2017, 05, 29);

            string? jsonData = null;
            int timeoutMs = 4000;
            Stopwatch sw = new();
            sw.Start();
            while (true)
            {
                var randomDateString = RandomDateString(start, end);

                string nytBaseUrl = @"https://raw.githubusercontent.com/doshea/nyt_crosswords/master";
                var nytUrl = string.Format("{0}/{1}.json", nytBaseUrl, randomDateString);

                try
                {
                    jsonData = await httpClient.GetStringAsync(nytUrl);
                    model = CrossWordTimes.FromJson(jsonData);
                    return model;
                }
                catch (WebException)
                {
                    // could not find an url, just try again
                }

                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    break;
                }
            }
            sw.Stop();
        }
        else
        {
            try
            {
                // url = "https://raw.githubusercontent.com/doshea/nyt_crosswords/master/1997/03/13.json";
                var jsonData = await httpClient.GetStringAsync(url);
                model = CrossWordTimes.FromJson(jsonData);
                return model;
            }
            catch (WebException e)
            {
                // could not find the url
                throw e;
            }
        }

        return null;
    }

    public static async Task<ICrossBoard> CreateFromFileAsync(string path)
    {
        await using var fs = File.OpenRead(path);
        return await CreateFromStreamAsync(fs);
    }

    public static async Task<ICrossBoard> CreateFromStreamAsync(Stream s)
    {
        var r = new StreamReader(s, Encoding.UTF8);
        var lines = new List<string>();
        while (true)
        {
            var line = await r.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) break;
            lines.Add(line);
        }

        int lineLength = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lineLength == -1)
                lineLength = lines[i].Length;
            else if (lines[i].Length != lineLength)
                throw new($"Line {i} has different length ({lines[i]}) then previous lines ({lineLength})");
        }

        ICrossBoard board = new CrossBoard(lineLength, lines.Count);
        for (int row = 0; row < lines.Count; row++)
        {
            var line = lines[row];
            for (int col = 0; col < lineLength; col++)
            {
                if (line[col] == '-')
                {
                    board.AddStartWord(col, row);
                }
            }
        }

        return board;
    }
}