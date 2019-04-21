using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrossWord.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using CrossWord.Web.Hubs;

namespace CrossWord.Web.Controllers
{
    public class SynonymsController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IApplicationLifetime _appLifetime;
        private readonly ILogger<SynonymsController> _logger;
        private readonly IConfiguration _appConfig;
        private readonly IHubContext<CrossWordsHub> _hubContext;

        public SynonymsController(
           IHostingEnvironment hostingEnvironment,
           IApplicationLifetime appLifetime,
           ILogger<SynonymsController> logger,
           IConfiguration configuration,
           IHubContext<CrossWordsHub> hubContext)
        {
            _hostingEnvironment = hostingEnvironment;
            _appLifetime = appLifetime;
            _logger = logger;
            _appConfig = configuration;
            _hubContext = hubContext;
        }

        [Route("synonyms")]
        public IActionResult Index()
        {
            // var apiBaseUrl = _appConfig["ApiBaseUrl"] ?? "http://order.wazalo.com:8000/api/";
            var apiBaseUrl = _appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var apiUserEmail = _appConfig["ApiUserEmail"] ?? "server@wazalo.com";
            var apiPassword = _appConfig["ApiPassword"] ?? "123ABCabc!";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["ApiUserEmail"] = apiUserEmail;
            ViewData["ApiPassword"] = apiPassword;

            return View();
        }

        [Route("synonyms/{word}")]
        public IActionResult Index(string word)
        {
            // var apiBaseUrl = _appConfig["ApiBaseUrl"] ?? "http://order.wazalo.com:8000/api/";
            var apiBaseUrl = _appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var apiUserEmail = _appConfig["ApiUserEmail"] ?? "server@wazalo.com";
            var apiPassword = _appConfig["ApiPassword"] ?? "123ABCabc!";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["ApiUserEmail"] = apiUserEmail;
            ViewData["ApiPassword"] = apiPassword;
            ViewData["Word"] = word;

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
