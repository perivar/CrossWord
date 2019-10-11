using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrossWord.Web.Controllers
{
    public class CrossWordController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IApplicationLifetime _appLifetime;
        private readonly ILogger<CrossWordController> _logger;
        private readonly IConfiguration _appConfig;

        public CrossWordController(
           IHostingEnvironment hostingEnvironment,
           IApplicationLifetime appLifetime,
           ILogger<CrossWordController> logger,
           IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _appLifetime = appLifetime;
            _logger = logger;
            _appConfig = configuration;
        }

        public IActionResult Index()
        {
            var signalRHubURL = _appConfig["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";
            ViewData["SignalRHubURL"] = signalRHubURL;

            return View();
        }
    }
}
