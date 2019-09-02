using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace Fantasy.Web
{
    public class Hubs : Hub<IComHub>
    {
        private readonly IMemoryCache memoryCache;

        public Hubs(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public async Task SendData(string data)
        {
            memoryCache.Set("teamcache", data);
            await Clients.All.ReceiveData(data);
        }
    }

    public interface IComHub
    {
        Task ReceiveData(string data);
    }
}