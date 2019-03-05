using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Web.Models;
using Microsoft.AspNetCore.SignalR;

namespace CrossWord.Web.Hubs
{
    public class CrossWordsHub : Hub
    {
        private static CancellationTokenSource CancelToken { get; set; }
        private static int counter = 0;

        public async Task AssociateJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
            await Broadcast("HUB", "job associated with " + jobId);
        }

        public async Task Broadcast(string name, string message)
        {
            await Clients
               // Do not Broadcast to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Broadcast to all connected clients:
               .SendAsync("Broadcast", name, message);
        }

        private async Task ReportProgressOld(ICrossBoard board)
        {
            var json = new CrossWordModel();
            json.Title = "" + counter++;

            await Clients.All.SendAsync("Progress", "HUB", json);
        }

        private void ReportProgress(ICrossBoard board)
        {
            var json = new CrossWordModel();
            json.Title = "" + counter++;

            Clients.All.SendAsync("Progress", "HUB", json);
        }

        public async Task CancelTaskOld()
        {
            await Clients.All.SendAsync("Broadcast", "HUB", "Cancelling Crossword Generation");
            if (CancelToken != null)
            {
                CancelToken.Cancel();
            }
        }

        public void CancelTask()
        {
            Clients.All.SendAsync("Broadcast", "HUB", "Cancelling Crossword Generation");
            if (CancelToken != null)
            {
                CancelToken.Cancel();
            }
        }

        public async Task<string> StartTask()
        {
            await Broadcast("HUB", "Starting Crossword Generation");

            // if (CancelToken == null) CancelToken = new CancellationTokenSource();
            if (CancelToken == null) CancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var token = CancelToken.Token;

            string inputFile = @"C:\Users\perner\My Projects\CrossWord\templates\american.txt";
            string dictionaryFile = @"C:\Users\perner\My Projects\CrossWord\dict\en";
            ICrossBoard board = CrossBoardCreator.CreateFromFile(inputFile);
            Dictionary dictionary = new Dictionary(dictionaryFile, board.MaxWordLength);

            // var task = Generator.GenerateCrossWords(board, dictionary, token, new Progress<ICrossBoard>(curBoard =>
            // {
            //     ReportProgress(curBoard);
            // }));

            try
            {
                var task = Task.Run(() =>
                {
                    var gen = new CrossGenerator(dictionary, board);
                    board.Preprocess(dictionary);

                    var crosswords = gen.Generate();
                    foreach (var resultBoard in crosswords)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }
                        ReportProgress(resultBoard);
                    }

                }, token);

                await task;
            }
            catch (OperationCanceledException)
            {
                // Cancel and timeout logic
                await Broadcast("HUB", "Crossword Generation Cancelled");
            }

            return "Task result";
        }
    }
}
