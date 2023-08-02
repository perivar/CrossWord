using CrossWord.Models;
using Microsoft.AspNetCore.SignalR;

namespace CrossWord.API.Hubs
{
    public class CrossWordSignalRHub : Hub
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

        public async Task SendStatus()
        {
            await Clients
               // Do not send to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Send to all connected clients:
               .SendAsync("SendStatus");
        }

        public async Task SendCrossword(string name, CrossWordTimes json)
        {
            await Clients
               // Do not send to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Send to all connected clients:
               .SendAsync("SendCrossword", name, json);
        }
    }
}
