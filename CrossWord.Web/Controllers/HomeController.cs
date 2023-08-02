using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CrossWord.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace CrossWord.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHostEnvironment hostingEnvironment;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ILogger<HomeController> logger;
        private readonly IConfiguration appConfig;

        public HomeController(
           IHostEnvironment  hostingEnvironment,
           IHostApplicationLifetime appLifetime,
           ILogger<HomeController> logger,
           IConfiguration configuration)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.appLifetime = appLifetime;
            this.logger = logger;
            appConfig = configuration;
        }

        public IActionResult Index()
        {
            // clear jwt token
            HttpContext.Session.Remove("token");

            var signalRHubURL = appConfig["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";
            ViewData["SignalRHubURL"] = signalRHubURL;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
