using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CrossWord
{
    public static class Generator
    {
        private const int MAX_GENERATOR_COUNT = 10;

        public static async Task GenerateCrosswordsAsync(ICrossBoard board, ICrossDictionary dictionary, string puzzle, string signalRHubURL, CancellationToken cancellationToken)
        {
            Log.Debug("GenerateCrosswordsAsync()");

            Log.Information("Trying to connect to SignalR Hub @ {0}", signalRHubURL);

            // Keep trying to until we can start
            HubConnection? hubConnection = null;
            while (true)
            {
                hubConnection = new HubConnectionBuilder()
                    .WithUrl(signalRHubURL)
                    .ConfigureLogging(logging =>
                    {
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddConsole();
                    })
                    .Build();

                try
                {
                    await hubConnection.StartAsync(cancellationToken);
                    await hubConnection.InvokeAsync("Broadcast", "Generator", "GenerateCrosswordsAsync");
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }

            Log.Information("Succesfully connected to SignalR Hub @ {0}", signalRHubURL);

            try
            {
                var generated = GenerateCrossWords(board, dictionary, puzzle, cancellationToken);
                int generatedCount = 0;
                foreach (var curCrossword in generated)
                {
                    generatedCount++;

                    var cb = curCrossword as CrossBoard;
                    var crossWordModel = cb.ToCrossWordModel(dictionary);
                    crossWordModel.Title = "Generated crossword number " + generatedCount;

                    Log.Debug("Succesfully converted generated crossword {0} to a Times model", generatedCount);

                    await hubConnection.InvokeAsync("SendCrossword", "Client", crossWordModel, cancellationToken);

                    await Task.Delay(50); // this makes the generation slower, can be removed
                    // break; // unncomment if we only want to use the first generated crossword
                }
            }
            catch (OperationCanceledException)
            {
                // Cancel and timeout logic
            }

            await hubConnection.DisposeAsync();

            Log.Information("Succesfully disconnected from the SignalR Hub @ {0}", signalRHubURL);
        }

        private static IEnumerable<ICrossBoard> GenerateCrossWords(ICrossBoard board, ICrossDictionary dictionary, string puzzle, CancellationToken cancellationToken)
        {
            if (puzzle != null)
            {
                Log.Information("Trying to generate crosswords for puzzle {0}", puzzle);

                var placer = new PuzzlePlacer(board, puzzle);
                foreach (var boardWithPuzzle in placer.GetAllPossiblePlacements(dictionary))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var gen = new CrossGenerator(dictionary, boardWithPuzzle);

                    // limit
                    int generatedCount = 0;

                    var generated = gen.Generate(cancellationToken);
                    foreach (var solution in generated)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        generatedCount++;

                        Log.Debug("Generated crossword {0}/{1}", generatedCount, MAX_GENERATOR_COUNT);

                        if (generatedCount >= MAX_GENERATOR_COUNT) break;

                        yield return solution;
                    }
                }
            }
            else
            {
                Log.Information("Trying to generate crosswords without a puzzle");

                var gen = new CrossGenerator(dictionary, board);
                board.Preprocess(dictionary);

                var crosswords = gen.Generate(cancellationToken);

                // limit
                int generatedCount = 0;

                foreach (var resultBoard in crosswords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    generatedCount++;

                    Log.Debug("Generated crossword {0}/{1}", generatedCount, MAX_GENERATOR_COUNT);

                    if (generatedCount >= MAX_GENERATOR_COUNT) break;

                    yield return resultBoard;
                }
            }
        }
    }
}