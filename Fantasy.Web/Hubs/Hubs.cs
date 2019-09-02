using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Fantasy.Web
{
    public class Hubs : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}