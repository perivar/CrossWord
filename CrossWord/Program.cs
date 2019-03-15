using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;

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
                using (var db = dbContextFactory.CreateDbContext(
                    new string[] { $"ConnectionStrings:DefaultConnection=server=localhost;database=dictionary;user=user;password=password;charset=utf8;"
                }))
                {
                    // setup database
                    // db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    // set admin user
                    var user = new User()
                    {
                        FirstName = "Admin",
                        LastName = "Admin",
                        UserName = "",
                        Password = "",
                        isVIP = true
                    };

                    // check if user already exists
                    var existingUser = db.Users.Where(o => o.FirstName == user.FirstName).FirstOrDefault();
                    if (existingUser != null)
                    {
                        user = existingUser;
                    }
                    else
                    {
                        db.Users.Add(user);
                        db.SaveChanges();
                    }

                    foreach (var dictElement in dictionary.Description)
                    {
                        var wordText = dictElement.Key.ToUpper();
                        var hintText = dictElement.Value.ToUpper();

                        var word = new Word
                        {
                            Language = "no",
                            Value = wordText,
                            NumberOfLetters = wordText.Count(c => c != ' '),
                            NumberOfWords = CrossWord.Scraper.Program.CountNumberOfWords(wordText),
                            User = user,
                            CreatedDate = DateTime.Now
                        };

                        // check if word already exists
                        var existingWord = db.Words.Where(o => o.Value == wordText).FirstOrDefault();
                        if (existingWord != null)
                        {
                            // update reference to existing word (reuse the word)
                            word = existingWord;
                        }
                        else
                        {
                            // add new word
                            db.Words.Add(word);
                            db.SaveChanges();
                        }

                        var hint = new Hint
                        {
                            Language = "no",
                            Value = hintText,
                            NumberOfLetters = hintText.Count(c => c != ' '),
                            NumberOfWords = CrossWord.Scraper.Program.CountNumberOfWords(hintText),
                            User = user,
                            CreatedDate = DateTime.Now
                        };

                        // check if hint already exists
                        bool skipHint = false;
                        var existingHint = db.Hints
                                            .Include(h => h.WordHints)
                                            .Where(o => o.Value == hintText).FirstOrDefault();
                        if (existingHint != null)
                        {
                            // update reference to existing hint (reuse the hint)
                            hint = existingHint;

                            // check if the current word already has been added as a reference to this hint
                            if (hint.WordHints.Count(h => h.WordId == word.WordId) > 0)
                            {
                                skipHint = true;
                            }
                        }
                        else
                        {
                            // add new hint
                            db.Hints.Add(hint);
                        }

                        if (!skipHint)
                        {
                            word.WordHints.Add(new WordHint()
                            {
                                Word = word,
                                Hint = hint
                            });

                            db.SaveChanges();

                            Console.WriteLine("Added '{0}' as a hint for '{1}'", hintText, word.Value);
                        }
                        else
                        {
                            Console.WriteLine("Skipped adding '{0}' as a hint for '{1}' ...", hintText, word.Value);
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