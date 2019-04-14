using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Scraper;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CrossWord
{
    static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("CrossWord ver. {0} ", "1.0");

            string inputFile, outputFile, puzzle, dictionaryFile;
            if (!ParseInput(args, out inputFile, out outputFile, out puzzle, out dictionaryFile))
            {
                return 1;
            }
            ICrossBoard board;
            try
            {
                board = CrossBoardCreator.CreateFromFile(inputFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Cannot load crossword layout from file {0}.", inputFile), e);
                return 2;
            }
            Dictionary dictionary;
            try
            {
                dictionary = new Dictionary(dictionaryFile, board.MaxWordLength);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Cannot load dictionary from file {0}.", dictionaryFile), e);
                return 3;
            }

            if (outputFile.Equals("signalr"))
            {
                // generate and send to signalr hub
                // var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var tokenSource = new CancellationTokenSource();
                Task workerTask = Task.Run(
                            async () =>
                            {
                                CancellationToken token = tokenSource.Token;
                                try
                                {
                                    await Generator.GenerateCrosswordsAsync(board, dictionary, puzzle, token);
                                }
                                catch (OperationCanceledException)
                                {
                                    Console.WriteLine("Cancelled @ {0}", DateTime.Now);
                                }
                            });

                // wait untill the task is done
                //Task.WaitAll(workerTask);

                // or wait until the user presses a key
                Console.WriteLine("Press Enter to Exit ...");
                Console.ReadLine();
                tokenSource.Cancel();
            }
            else if (outputFile.Equals("database"))
            {
                var dbContextFactory = new DesignTimeDbContextFactory();
                using (var db = dbContextFactory.CreateDbContext("server=localhost;database=dictionary;user=user;password=password;charset=utf8;", Log.Logger)) // null instead of Log.Logger enables debugging
                {
                    // setup database
                    // You would either call EnsureCreated() or Migrate(). 
                    // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                    // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                    // db.Database.EnsureDeleted();
                    // db.Database.EnsureCreated();

                    // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                    db.Database.Migrate();

                    // set admin user
                    var user = new User()
                    {
                        FirstName = "Admin",
                        LastName = "Admin",
                        UserName = "",
                        isVIP = true
                    };

                    // check if user already exists
                    var existingUser = db.DictionaryUsers.Where(u => u.FirstName == user.FirstName).FirstOrDefault();
                    if (existingUser != null)
                    {
                        user = existingUser;
                    }
                    else
                    {
                        db.DictionaryUsers.Add(user);
                        db.SaveChanges();
                    }

                    if (Path.GetExtension(dictionaryFile).ToLower().Equals(".json"))
                    {
                        // read json files
                        using (StreamReader r = new StreamReader(dictionaryFile))
                        {
                            var json = r.ReadToEnd();
                            var jobj = JObject.Parse(json);

                            var totalCount = jobj.Properties().Count();
                            int count = 0;
                            foreach (var item in jobj.Properties())
                            {
                                count++;

                                var wordText = item.Name;
                                var relatedArray = item.Values().Select(a => a.Value<string>());

                                doAddToDatabase(db, user, wordText, relatedArray);

                                Console.WriteLine("[{0}] / [{1}]", count, totalCount);
                            }
                        }
                    }
                }
            }
            else
            {
                ICrossBoard resultBoard;
                try
                {
                    resultBoard = puzzle != null
                        ? GenerateFirstCrossWord(board, dictionary, puzzle)
                        : GenerateFirstCrossWord(board, dictionary);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Generating crossword has failed.", e);
                    return 4;
                }
                if (resultBoard == null)
                {
                    Console.WriteLine(string.Format("No solution has been found."));
                    return 5;
                }
                try
                {
                    SaveResultToFile(outputFile, resultBoard, dictionary);
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Saving result crossword to file {0} has failed.", outputFile), e);
                    return 6;
                }
            }
            return 0;
        }

        static void doAddToDatabase(WordHintDbContext db, User user, string wordText, IEnumerable<string> relatedValues)
        {
            // disable tracking to speed things up
            // note that this doesn't load the virtual properties, but loads the object ids after a save
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // ensure uppercase
            wordText = wordText.ToUpper();

            var word = new Word
            {
                Language = "no",
                Value = wordText,
                NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText),
                NumberOfWords = ScraperUtils.CountNumberOfWords(wordText),
                User = user,
                CreatedDate = DateTime.Now
            };

            // check if word already exists
            var existingWord = db.Words.Where(w => w.Value == wordText).FirstOrDefault();
            if (existingWord != null)
            {
                // update reference to existing word (i.e. reuse the word)
                word = existingWord;
            }
            else
            {
                // add new word
                db.Words.Add(word);
                db.SaveChanges();
            }

            // ensure related are all uppercase and distinct
            var relatedValuesUpperCase = relatedValues.Select(x => x.ToUpper()).Distinct();

            // get all related words (hints) as Word objects
            var relatedWords = relatedValuesUpperCase.Select(hintText => new Word
            {
                Language = "no",
                Value = hintText,
                NumberOfLetters = ScraperUtils.CountNumberOfLetters(hintText),
                NumberOfWords = ScraperUtils.CountNumberOfWords(hintText),
                User = user,
                CreatedDate = DateTime.Now
            });

            // find out which relatedwords already exist in the database
            var existingRelatedWords = db.Words.Where(x => relatedValuesUpperCase.Contains(x.Value)).ToList();

            // which words need to be added?
            var newRelatedWords = relatedWords.Where(x => !existingRelatedWords.Any(a => a.Value == x.Value)).ToList();

            if (newRelatedWords.Count > 0)
            {
                db.Words.AddRange(newRelatedWords);
                db.SaveChanges();
                // Console.WriteLine("Added '{0}' ...", string.Join(",", newRelatedWords.Select(i => i.Value).ToArray()));
            }
            else
            {
                // Console.WriteLine("Skipped adding '{0}' ...", string.Join(",", existingRelatedWords.Select(i => i.Value).ToArray()));
            }

            // what relations needs to be added?
            var allRelatedWords = existingRelatedWords.Concat(newRelatedWords);
            var allWordRelations = allRelatedWords.Select(hint =>
                // only use the ids to speed things up
                new WordRelation { WordFromId = word.WordId, WordToId = hint.WordId }
            );

            // find out which relations already exist in the database
            // check both directions in the Many-to-Many Relationship 
            var allRelatedWordsIds = allRelatedWords.Select(a => a.WordId).ToList();
            var existingWordRelations = db.WordRelations.Where(a =>
                (a.WordFromId == word.WordId && allRelatedWordsIds.Contains(a.WordToId))
                ||
                (a.WordToId == word.WordId && allRelatedWordsIds.Contains(a.WordFromId))
            ).ToList();

            // which relations need to be added?
            // check both directions in the Many-to-Many Relationship             
            var newWordRelations = allWordRelations.Where(wr => !existingWordRelations.Any
            (a =>
                (a.WordFromId == wr.WordFromId && a.WordToId == wr.WordToId)
                ||
                (a.WordFromId == wr.WordToId && a.WordToId == wr.WordFromId)
            )).ToList();

            if (newWordRelations.Count > 0)
            {
                db.WordRelations.AddRange(newWordRelations);
                db.SaveChanges();

                // with tracking we can output the actual words
                // Console.WriteLine("Added '{0}' to '{1}' ...", string.Join(",", newRelations.Select(i => i.WordTo.Value).ToArray()), wordText);

                // without tracking we don't have the word value, so use only the wordids
                // Console.WriteLine("Added '{0}' to '{1}' ...", string.Join(",", newRelations.Select(i => i.WordToId).ToArray()), wordText);
            }
            else
            {
                // with tracking we can output the actual words
                // Console.WriteLine("Skipped relating '{0}' to '{1}' ...", string.Join(",", existingRelations.Select(i => i.WordTo.Value).ToArray()), wordText);

                // without tracking we don't have the word value, so use only the wordids
                // Console.WriteLine("Skipped relating '{0}' to '{1}' ...", string.Join(",", existingRelations.Select(i => i.WordToId).ToArray()), wordText);
            }
        }

        static bool ParseInput(IEnumerable<string> args, out string inputFile, out string outputFile, out string puzzle,
            out string dictionary)
        {
            bool help = false;
            string i = null, o = null, p = null, d = null;
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
            if (help || unparsed.Count > 1 || string.IsNullOrEmpty(inputFile) ||
                string.IsNullOrEmpty(outputFile) || string.IsNullOrEmpty(dictionary))
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return false;
            }
            return true;
        }

        static ICrossBoard GenerateFirstCrossWord(ICrossBoard board, ICrossDictionary dictionary)
        {
            var gen = new CrossGenerator(dictionary, board);
            board.Preprocess(dictionary);

            return gen.Generate().FirstOrDefault();
        }

        static ICrossBoard GenerateFirstCrossWord(ICrossBoard board, ICrossDictionary dictionary, string puzzle)
        {
            var placer = new PuzzlePlacer(board, puzzle);
            var cts = new CancellationTokenSource();
            var mre = new ManualResetEvent(false);
            ICrossBoard successFullBoard = null;
            foreach (var boardWithPuzzle in placer.GetAllPossiblePlacements(dictionary))
            {
                // boardWithPuzzle.WriteTo(new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding) { AutoFlush = true });
                var gen = new CrossGenerator(dictionary, boardWithPuzzle);
                var t = Task.Factory.StartNew(() =>
                                          {
                                              foreach (var solution in gen.Generate())
                                              {
                                                  successFullBoard = solution;
                                                  cts.Cancel();
                                                  mre.Set();
                                                  break; //interested in the first one
                                              }
                                          }, cts.Token);
                if (cts.IsCancellationRequested)
                    break;
            }
            mre.WaitOne();
            return successFullBoard;
        }

        static void SaveResultToFile(string outputFile, ICrossBoard resultBoard, ICrossDictionary dictionary)
        {
            Console.WriteLine("Solution has been found:");
            using (var writer = new StreamWriter(new FileStream(outputFile, FileMode.Create)))
            {
                resultBoard.WriteTo(writer);
                resultBoard.WritePatternsTo(writer, dictionary);
            }
        }
    }
}