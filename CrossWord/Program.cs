using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CrossWord
{
    static class Program
    {
        public static int Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                                    .AddCommandLine(args)
                                    .AddEnvironmentVariables()
                                    .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Information("Starting Puzzle generator app ver. {0} ", "1.0");

            if (!ParseInput(args, out string inputFile, out string outputFile, out string dictionaryFile, out string? puzzle))
            {
                return 1;
            }
            ICrossBoard board;
            try
            {
                if (inputFile.StartsWith("http"))
                {
                    board = CrossBoardCreator.CreateFromUrlAsync(inputFile).Result;
                }
                else
                {
                    board = CrossBoardCreator.CreateFromFileAsync(inputFile).Result;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Cannot load crossword layout from file {inputFile}.");
                return 2;
            }

            ICrossDictionary dictionary;
            try
            {
                if (dictionaryFile.Equals("database"))
                {
                    const string CONNECTION_STRING_KEY = "DefaultConnection";
                    var connectionString = configuration.GetConnectionString(CONNECTION_STRING_KEY);
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new Exception(string.Format("Cannot use database without a configured connection string! (looking for key: {0})", CONNECTION_STRING_KEY));
                    }
                    else
                    {
                        Log.Information("Using database with connection string: {0}", connectionString);
                        var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);
                        dictionary = new DatabaseDictionary(connectionString, board.MaxWordLength, loggerFactory);
                    }
                }
                else
                {
                    dictionary = new Dictionary(dictionaryFile, board.MaxWordLength);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Cannot load dictionary from file {dictionaryFile}.");
                return 3;
            }

            if (outputFile.Equals("signalr"))
            {
                try
                {
                    // generate and send to signalr hub
                    // var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var tokenSource = new CancellationTokenSource();
                    Task workerTask = Task.Run(
                                async () =>
                                {
                                    try
                                    {
                                        var signalRHubURL = configuration["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";
                                        await Generator.GenerateCrosswordsAsync(board, dictionary, puzzle, signalRHubURL, tokenSource);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        Log.Information("Cancelled @ {0}", DateTime.Now);
                                    }
                                });

                    // wait until the task is done
                    workerTask.Wait();

                    // or wait until the user presses a key
                    // Console.WriteLine("Press Enter to Exit ...");
                    // Console.ReadLine();
                    // tokenSource.Cancel();
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Failed generating crossword asynchronously");
                    return 4;
                }
            }
            else if (outputFile.Equals("database"))
            {
                string source = "norwegian-synonyms.json";

                // set admin user
                var adminUser = new User()
                {
                    FirstName = "",
                    LastName = "Norwegian Synonyms json",
                    UserName = "norwegian-synonyms.json"
                };

                OutputToDatabase(dictionaryFile, source, adminUser);
            }
            else
            {
                ICrossBoard? resultBoard;
                try
                {
                    resultBoard = puzzle != null
                        ? GenerateFirstCrossWord(board, dictionary, puzzle)
                        : GenerateFirstCrossWord(board, dictionary);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Generating crossword has failed.");
                    return 5;
                }

                if (resultBoard == null)
                {
                    Log.Error("No solution has been found.");
                    return 6;
                }

                try
                {
                    SaveResultToFile(outputFile, resultBoard, dictionary);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Saving result crossword to file {outputFile} has failed.");
                    return 7;
                }
            }
            return 0;
        }

        private static void OutputToDatabase(string dictionaryFile, string source, User adminUser)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using var db = dbContextFactory.CreateDbContext("server=localhost;database=dictionary;user=user;password=password;charset=utf8;", Log.Logger); // null instead of Log.Logger enables debugging

            // setup database
            // You would either call EnsureCreated() or Migrate(). 
            // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
            // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
            // db.Database.EnsureDeleted();
            // db.Database.EnsureCreated();

            // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
            db.Database.Migrate();

            // check if admin user already exists
            var existingUser = db.DictionaryUsers.Where(u => u.FirstName == adminUser.FirstName).FirstOrDefault();
            if (existingUser != null)
            {
                adminUser = existingUser;
            }
            else
            {
                db.DictionaryUsers.Add(adminUser);
                db.SaveChanges();
            }

            // disable tracking to speed things up
            // note that this doesn't load the virtual properties, but loads the object ids after a save
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // this works when using the same user for all words.
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            bool isDebugging = false;
#if DEBUG
            isDebugging = true;
#endif

            if (Path.GetExtension(dictionaryFile).ToLower().Equals(".json"))
            {
                // read json files
                using StreamReader r = new(dictionaryFile);
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);

                var totalCount = jobj.Properties().Count();
                int count = 0;
                foreach (var item in jobj.Properties())
                {
                    count++;

                    var wordText = item.Name;
                    var relatedArray = item.Values().Select(a => a.Value<string>());

                    WordDatabaseService.AddToDatabase(db, source, adminUser, wordText, relatedArray);

                    if (isDebugging)
                    {
                        // in debug mode the Console.Write \r isn't shown in the output console
                        Console.WriteLine("[{0}] / [{1}]", count, totalCount);
                    }
                    else
                    {
                        Console.Write("\r[{0}] / [{1}]", count, totalCount);
                    }
                }
                Console.WriteLine("Done!");
            }
        }

        static bool ParseInput(IEnumerable<string> args, out string inputFile, out string outputFile, out string dictionary, out string? puzzle)
        {
            bool help = false;
            string? i = null, o = null, p = null, d = null;
            var optionSet = new NDesk.Options.OptionSet
                                {
                                    { "i|input=", "(input file)", v => i = v },
                                    { "d|dictionary=", "(dictionary)", v => d = v },
                                    { "o|output=", "(output file)", v => o = v },
                                    { "p|puzzle=", "(puzze)", v => p = v },
                                    { "h|?|help", "(help)", v => help = v != null },
                                };
            var unparsed = optionSet.Parse(args);
            inputFile = i;
            outputFile = o;
            puzzle = p;
            dictionary = d;
            if (help || unparsed.Count > 1 || string.IsNullOrEmpty(i) ||
                string.IsNullOrEmpty(o) || string.IsNullOrEmpty(d))
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return false;
            }
            return true;
        }

        static ICrossBoard? GenerateFirstCrossWord(ICrossBoard board, ICrossDictionary dictionary)
        {
            var gen = new CrossGenerator(dictionary, board);
            board.Preprocess(dictionary);
            return gen.Generate(CancellationToken.None).FirstOrDefault();
        }

        static ICrossBoard GenerateFirstCrossWord(ICrossBoard board, ICrossDictionary dictionary, string puzzle)
        {
            var placer = new PuzzlePlacer(board, puzzle);
            var cts = new CancellationTokenSource();
            var mre = new ManualResetEvent(false);
            ICrossBoard? successFullBoard = null;
            foreach (var boardWithPuzzle in placer.GetAllPossiblePlacements(dictionary))
            {
                var gen = new CrossGenerator(dictionary, boardWithPuzzle);
                var _ = Task.Factory.StartNew(() =>
                {
                    var solution = gen.Generate(cts.Token).FirstOrDefault();
                    if (solution != null)
                    {
                        successFullBoard = solution;
                        cts.Cancel();
                        mre.Set();
                    }

                }, cts.Token);
                if (cts.IsCancellationRequested)
                    break;
            }

            mre.WaitOne();
            return successFullBoard!;
        }

        static void SaveResultToFile(string outputFile, ICrossBoard resultBoard, ICrossDictionary dictionary)
        {
            Console.WriteLine($"Solution has been writen to file {outputFile}.");
            using var writer = new StreamWriter(new FileStream(outputFile, FileMode.Create));
            resultBoard.WriteTo(writer);
            resultBoard.WritePatternsTo(writer, dictionary);
        }
    }
}