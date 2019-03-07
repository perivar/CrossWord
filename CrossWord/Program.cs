using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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