using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Models;
using Microsoft.AspNetCore.SignalR;

namespace CrossWord.Web.Hubs
{
    public class CrossWordsHub : Hub
    {
        public async Task Broadcast(string name, string message)
        {
            await Clients
               // Do not Broadcast to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Broadcast to all connected clients:
               .SendAsync("Broadcast", name, message);
        }

        public async Task BroadcastAll(string user, string message)
        {
            await Clients.All.SendAsync("Broadcast", user, message);
        }

        public async Task SendCrossword(string name, CrossWordModel json)
        {
            await Clients
               // Do not send to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Send to all connected clients:
               .SendAsync("SendCrossword", name, json);
        }

        public async Task StartTask()
        {
            await Clients.All.SendAsync("Broadcast", "HUB", "Starting Task...");
        }

        public async Task CancelTask()
        {
            await Clients.All.SendAsync("Broadcast", "HUB", "Cancelling Task...");
        }
    }
}
