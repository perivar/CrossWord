using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;

namespace CrossWord.Scraper
{
    public class SignalRClientWriter : TextWriter
    {
        private HubConnection SignalRConnection { get; set; }
        private bool FlushAfterEveryWrite { get; set; }
        private string Identifier { get; set; }
        public string ExtraStatusInformation { get; set; }

        public SignalRClientWriter(string url) : this(url, null)
        {
        }

        public SignalRClientWriter(string url, string identifier)
        {
            this.FlushAfterEveryWrite = false;
            this.Identifier = identifier != null ? identifier : "Robot";

            // https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-2.1
            SignalRConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Critical);

                // Add Serilog
                // make sure it has been configured first, like this somewhere
                // Log.Logger = new LoggerConfiguration()
                // ...
                // .CreateLogger();
                logging.AddSerilog(dispose: true);
            })
            .Build();

            SignalRConnection.On("SendStatus", () =>
           {
               var id = Thread.CurrentThread.ManagedThreadId;
               var state = Thread.CurrentThread.ThreadState;
               var priority = Thread.CurrentThread.Priority;
               var isAlive = Thread.CurrentThread.IsAlive;

               SignalRConnection.InvokeAsync("Broadcast", Identifier, $"Thread: {id}, state: {state}, priority: {priority}, isAlive: {isAlive}");
               if (ExtraStatusInformation != null) SignalRConnection.InvokeAsync("Broadcast", Identifier, $"Extra Information: {ExtraStatusInformation}");
           });

            // Ingore Broadcast events from the other SignalRClientWriter
            // if we don't add this we get an error each time we receive a method this client doesn't know how to deal with.            
            // i.e the Broadcast methods called from the Hub: Clients.All.SendAsync("Broadcast", user, message);
            SignalRConnection.On<string, string>("Broadcast", (user, message) =>
            {
                // ignore
            });

            // open connection
            // use auto reconnect from here:
            // https://www.radenkozec.com/net-core-signalr-automatic-reconnects/
            OpenSignalRConnection();
        }

        private async void OpenSignalRConnection()
        {
            var pauseBetweenFailures = TimeSpan.FromSeconds(10);
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(i => pauseBetweenFailures,
                (exception, timeSpan) =>
                {
                    Log.Error(exception.Message);
                });

            await retryPolicy.ExecuteAsync(async () =>
            {
                Log.Information("Trying to connect to SignalR server");
                await TryOpenSignalRConnection();
            });
        }

        private async Task TryOpenSignalRConnection()
        {
            Log.Information("Starting SignalR connection");

            // this will throw an exception if it doesn't work and will be picked up by Polly's single exception type - Policy.Handle<Exception>()
            await SignalRConnection.StartAsync();

            // subscribe to the closed event
            SignalRConnection.Closed += SignalRConnection_Closed;

            Log.Information("SignalR connection established");
        }

        private async Task SignalRConnection_Closed(Exception arg)
        {
            Log.Information("SignalR connection is closed");
            await SignalRConnection.StopAsync();

            // unsubscribe so we don't have many many concurrent subscriptions to the same event for each 
            // time we try to subscribe in TryOpenSignalRConnection()
            SignalRConnection.Closed -= SignalRConnection_Closed;

            OpenSignalRConnection();
        }

        public override Encoding Encoding => throw new System.NotImplementedException();

        public override async Task WriteAsync(string value)
        {
            await OutputMessage(value);

            if (FlushAfterEveryWrite)
            {
                await FlushAsync();
            }
        }

        public override async Task WriteLineAsync(string value)
        {
            await OutputMessage(value);

            if (FlushAfterEveryWrite)
            {
                await FlushAsync();
            }
        }

        public override async Task WriteLineAsync()
        {
            await OutputMessage(null);

            if (FlushAfterEveryWrite)
            {
                await FlushAsync();
            }
        }

        public override async Task FlushAsync()
        {
            // do nothing
        }

        public override void Write(string value)
        {
            OutputMessage(value).GetAwaiter();

            if (FlushAfterEveryWrite)
            {
                Flush();
            }
        }

        public override void WriteLine(string value)
        {
            OutputMessage(value).GetAwaiter();

            if (FlushAfterEveryWrite)
            {
                Flush();
            }
        }

        public override void WriteLine()
        {
            OutputMessage(null).GetAwaiter();

            if (FlushAfterEveryWrite)
            {
                Flush();
            }
        }

        public override void Flush()
        {
            // do nothing
        }

        private async Task OutputMessage(string message)
        {
            try
            {
                await SignalRConnection.InvokeAsync("Broadcast", Identifier, message);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Failed sending message to SignalR Hub: {0}", ex.Message));
            }
        }
    }
}