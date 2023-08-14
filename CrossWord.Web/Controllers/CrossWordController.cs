using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrossWord.Web.Controllers
{
    public class CrossWordController : Controller
    {
        private readonly IHostEnvironment hostingEnvironment;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ILogger<CrossWordController> logger;
        private readonly IConfiguration appConfig;

        public CrossWordController(
           IHostEnvironment  hostingEnvironment,
           IHostApplicationLifetime appLifetime,
           ILogger<CrossWordController> logger,
           IConfiguration configuration)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.appLifetime = appLifetime;
            this.logger = logger;
            appConfig = configuration;
        }

        public IActionResult Index()
        {
            var apiBaseUrl = appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var signalRHubURL = appConfig["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["SignalRHubURL"] = signalRHubURL;

            return View();
        }
    }
}
