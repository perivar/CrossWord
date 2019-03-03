using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace CrossWordWeb.Hubs
{
    public class CrossWordsHub : Hub
    {
        public async Task AssociateJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
            await Clients.All.SendAsync("Broadcast", "HUB", "job associated with " + jobId);
        }

        public async Task Broadcast(string name, string message)
        {
            await Clients
               // Do not Broadcast to Caller:
               .AllExcept(new[] { Context.ConnectionId })
               // Broadcast to all connected clients:
               .SendAsync("Broadcast", name, message);
        }
    }
}
