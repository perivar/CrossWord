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
using System.Net.Http;
using System.Net;

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
            ViewData["Token"] = GetToken(apiBaseUrl, apiUserEmail, apiPassword);

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
            ViewData["Token"] = GetToken(apiBaseUrl, apiUserEmail, apiPassword);

            return View();
        }

        private string GetToken(string apiBaseUrl, string apiUserEmail, string apiPassword)
        {
            var authUrl = $"{apiBaseUrl}Account/Login?email={apiUserEmail}&password={apiPassword}";

            string token = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, authUrl))
                {
                    // var stringContent = new StringContent(JsonConvert.SerializeObject(TestAPIHelper.loginObject), Encoding.UTF8, "application/json");
                    // request.Content = stringContent;

                    using (HttpResponseMessage response = httpClient.SendAsync(request, System.Threading.CancellationToken.None).Result)
                    {
                        token = response.Content.ReadAsAsync<string>().Result;
                    }
                }
            }

            return token;
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
