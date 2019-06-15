using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using CrossWord.Models;

namespace CrossWord
{
    static public class CrossBoardCreator
    {
        public static ICrossBoard CreateFromUrl(string url)
        {
            var model = GetCrossWordModelFromUrl(url);
            var board = model.ToCrossBoard();

            return board;
        }

        private static string RandomDateString(DateTime startDate, DateTime endDate)
        {
            TimeSpan timeSpan = endDate - startDate;
            var randomTest = new Random();
            TimeSpan newSpan = new TimeSpan(0, randomTest.Next(0, (int)timeSpan.TotalMinutes), 0);
            DateTime newDate = startDate + newSpan;

            return string.Format(CultureInfo.InvariantCulture, "{0:yyyy/MM/dd}", newDate);
        }

        public static CrossWordTimes GetCrossWordModelFromUrl(string url)
        {
            CrossWordTimes model = null;
            using (WebClient httpClient = new WebClient())
            {
                if (url.ToLower().Equals("http-random"))
                {
                    var start = new DateTime(1976, 01, 01);
                    var end = new DateTime(2017, 05, 29);

                    string jsonData = null;
                    int timeoutMs = 4000;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (true)
                    {
                        var randomDateString = RandomDateString(start, end);

                        string nytBaseUrl = @"https://raw.githubusercontent.com/doshea/nyt_crosswords/master";
                        var nytUrl = string.Format("{0}/{1}.json", nytBaseUrl, randomDateString);

                        try
                        {
                            jsonData = httpClient.DownloadString(nytUrl);
                            break;
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

                    model = CrossWordTimes.FromJson(jsonData);
                }
                else
                {
                    try
                    {
                        // url = "https://raw.githubusercontent.com/doshea/nyt_crosswords/master/1997/03/13.json";
                        var jsonData = httpClient.DownloadString(url);
                        model = CrossWordTimes.FromJson(jsonData);
                    }
                    catch (WebException e)
                    {
                        // could not find the url
                        throw e;
                    }
                }
            }
            return model;
        }

        public static ICrossBoard CreateFromFile(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                return CreateFromStream(fs);
            }
        }

        public static ICrossBoard CreateFromStream(Stream s)
        {
            var r = new StreamReader(s, Encoding.UTF8);
            var lines = new List<string>();
            while (true)
            {
                var line = r.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                lines.Add(line);
            }
            int lineLength = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lineLength == -1)
                {
                    lineLength = lines[i].Length;
                }
                else if (lines[i].Length != lineLength)
                {
                    throw new Exception(string.Format("Line {0} has different length ({1}) then previous lines ({2})",
                        i, lines[i], lineLength));
                }
            }
            var board = new CrossBoard();
            board.SetBoardSize(lineLength, lines.Count);
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
}
