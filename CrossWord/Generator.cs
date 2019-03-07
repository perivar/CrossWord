using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace CrossWord
{
    public static class Generator
    {
        public static async Task GenerateCrosswordsAsync(ICrossBoard board, ICrossDictionary dictionary, string puzzle, CancellationToken cancellationToken)
        {
            // Keep trying to until we can start
            HubConnection hubConnection = null;
            while (true)
            {
                hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5000/crosswords")
                    .ConfigureLogging(logging =>
                    {
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddConsole();
                    })
                    .Build();

                try
                {
                    await hubConnection.StartAsync();
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                }
            }

            try
            {
                var generated = GenerateCrossWords(board, dictionary, puzzle, cancellationToken);
                foreach (var curCrossword in generated)
                {
                    var cb = curCrossword as CrossBoard;
                    var crossWordModel = cb.ToCrossWordModel(dictionary);
                    await hubConnection.InvokeAsync("SendCrossword", "Client", crossWordModel, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancel and timeout logic
            }

            await hubConnection.DisposeAsync();
        }

        private static IEnumerable<ICrossBoard> GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, CancellationToken cancellationToken)
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

        private static IEnumerable<ICrossBoard> GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, string puzzle, CancellationToken cancellationToken)
        {
            if (puzzle != null)
            {
                var placer = new PuzzlePlacer(board, puzzle);
                foreach (var boardWithPuzzle in placer.GetAllPossiblePlacements(dictionary))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var gen = new CrossGenerator(dictionary, boardWithPuzzle);

                    foreach (var solution in gen.Generate())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return solution;
                    }
                }
            }
            else
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
        }
    }
}