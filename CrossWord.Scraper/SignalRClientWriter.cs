using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace CrossWord.Scraper
{
    public class SignalRClientWriter : TextWriter
    {
        private HubConnection HubConnection { get; set; }
        private bool HubConnectionStarted { get; set; }
        private bool FlushAfterEveryWrite { get; set; }
        private string Identifier { get; set; }

        public SignalRClientWriter(string url) : this(url, null)
        {
        }

        public SignalRClientWriter(string url, string identifier)
        {
            this.HubConnectionStarted = false;
            this.FlushAfterEveryWrite = false;
            this.Identifier = identifier != null ? identifier : "Robot";

            // https://docs.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-2.1
            HubConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Critical);
                logging.AddConsole();
            })
            .Build();

            HubConnection.On("SendStatus", () =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                var state = Thread.CurrentThread.ThreadState;
                var priority = Thread.CurrentThread.Priority;

                HubConnection.InvokeAsync("Broadcast", Identifier, $"Thread: {id}, state: {state}, priority: {priority}");
            });

            // support self-signed SSL certificates - not working therefore disabled
            // ServicePointManager.ServerCertificateValidationCallback +=
            //       (sender, certificate, chain, sslPolicyErrors) => true;

            // open connection
            CheckOrOpenConnection().Wait();
        }

        public override async Task WriteAsync(string value)
        {
            await OutputMessage(value);

            if (FlushAfterEveryWrite)
                await FlushAsync();
        }

        public override async Task WriteLineAsync(string value)
        {
            await OutputMessage(value);

            if (FlushAfterEveryWrite)
                await FlushAsync();
        }

        public override async Task WriteLineAsync()
        {
            await OutputMessage(null);

            if (FlushAfterEveryWrite)
                await FlushAsync();
        }

        public override async Task FlushAsync()
        {
            // do nothing
        }

        public override void Write(string value)
        {
            OutputMessage(value).Wait();

            if (FlushAfterEveryWrite)
                Flush();
        }

        public override void WriteLine(string value)
        {
            OutputMessage(value).Wait();

            if (FlushAfterEveryWrite)
                Flush();
        }

        public override void WriteLine()
        {
            OutputMessage(null).Wait();

            if (FlushAfterEveryWrite)
                Flush();
        }

        public override void Flush()
        {
            // do nothing
        }

        public override Encoding Encoding => throw new System.NotImplementedException();

        private async Task CheckOrOpenConnection()
        {
            if (!HubConnectionStarted)
            {
                try
                {
                    await HubConnection.StartAsync();
                    HubConnectionStarted = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed starting SignalR client: {0}", ex.Message);
                }
            }
        }

        private async Task OutputMessage(string message)
        {
            await CheckOrOpenConnection();

            if (HubConnectionStarted)
            {
                try
                {
                    await HubConnection.InvokeAsync("Broadcast", Identifier, message);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed sending message to SignalR Hub: {0}", ex.Message);
                }
            }

            // Console.WriteLine(message);
        }
    }
}