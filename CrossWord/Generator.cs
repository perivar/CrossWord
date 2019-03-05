using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CrossWord
{
    public static class Generator
    {
        public static IEnumerable<ICrossBoard> GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, CancellationToken cancellationToken)
        {
            var gen = new CrossGenerator(dictionary, board);
            board.Preprocess(dictionary);

            var crosswords = gen.Generate();
            foreach (var resultBoard in crosswords)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return resultBoard;
            }
        }

        public static async Task GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, CancellationToken cancellationToken, IProgress<ICrossBoard> progress)
        {
            var gen = new CrossGenerator(dictionary, board);
            board.Preprocess(dictionary);

            foreach (var resultBoard in gen.Generate())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // report which number of crossword we have generated
                if (progress != null)
                {
                    progress.Report(resultBoard);
                }

                // await System.Threading.Tasks.Task.Delay(100);
            }

            // for (int i = 0; i <= 100; i++)
            // {
            //     if (cancellationToken.IsCancellationRequested)
            //     {
            //         cancellationToken.ThrowIfCancellationRequested();
            //     }
            //     if (progress != null)
            //     {
            //         var curCrossBoard = new CrossBoard();
            //         progress.Report(curCrossBoard);
            //     }

            //     await System.Threading.Tasks.Task.Delay(1000);
            // }

            // if (progress != null)
            // {
            //     var curCrossBoard = new CrossBoard();
            //     progress.Report(curCrossBoard);
            // }
        }

        public static ICrossBoard GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, string puzzle)
        {
            var placer = new PuzzlePlacer(board, puzzle);
            var cts = new CancellationTokenSource();
            var mre = new ManualResetEvent(false);
            ICrossBoard successFullBoard = null;
            foreach (var boardWithPuzzle in placer.GetAllPossiblePlacements(dictionary))
            {
                var gen = new CrossGenerator(dictionary, boardWithPuzzle);

                var t = Task.Factory.StartNew(() =>
                                          {
                                              foreach (var solution in gen.Generate())
                                              {
                                                  successFullBoard = solution;
                                                  cts.Cancel();
                                                  mre.Set();
                                                  break; // interested in the first one
                                              }
                                          }, cts.Token);

                if (cts.IsCancellationRequested)
                    break;
            }
            mre.WaitOne();
            return successFullBoard;
        }
    }
}