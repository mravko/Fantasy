using Fantasy.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fantasy.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<Hubs, IComHub> hubContext;

        public HomeController(IHubContext<Hubs, IComHub> hubContext)
        {
            this.hubContext = hubContext;
        }
        public async Task<IActionResult> Index()
        {
            await hubContext.Clients.All.ReceiveData("hello");

            return View();
        }

        public async Task<IActionResult> Privacy()
        {   
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    [ApiController]
    [Route("api/")]
    public class TeamController : ControllerBase
    {
        private readonly IMemoryCache memoryCache;

        public TeamController(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        [HttpGet()]
        public ActionResult<object> GetTeamFromCache()
        {
            return memoryCache.Get("teamcache");
        }
    }
}
