using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CrossWord.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace CrossWord.Web.Controllers
{
    public class SynonymsController : Controller
    {
        private readonly IHostEnvironment hostingEnvironment;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ILogger<SynonymsController> logger;
        private readonly IConfiguration appConfig;

        public SynonymsController(
           IHostEnvironment  hostingEnvironment,
           IHostApplicationLifetime appLifetime,
           ILogger<SynonymsController> logger,
           IConfiguration configuration)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.appLifetime = appLifetime;
            this.logger = logger;
            appConfig = configuration;
        }

        [Route("synonyms")]
        public IActionResult Index()
        {
            var apiBaseUrl = appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var apiBaseLocalUrl = appConfig["ApiBaseLocalUrl"] ?? apiBaseUrl; // to support localhost in docker
            var apiUserEmail = appConfig["ApiUserEmail"] ?? "server@wazalo.com";
            var apiPassword = appConfig["ApiPassword"] ?? "123ABCabc!";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["ApiUserEmail"] = apiUserEmail;
            ViewData["ApiPassword"] = apiPassword;

            string token;
            if (HttpContext.Session.GetString("token") == null)
            {
                token = GetJwtToken(apiBaseLocalUrl, apiUserEmail, apiPassword);
                if (token != null) HttpContext.Session.SetString("token", token);
            }
            else
            {
                token = HttpContext.Session.GetString("token");
            }
            ViewData["Token"] = token;

            return View();
        }

        [Route("synonyms/{word}")]
        public IActionResult Index(string word)
        {
            var apiBaseUrl = appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var apiBaseLocalUrl = appConfig["ApiBaseLocalUrl"] ?? apiBaseUrl; // to support localhost in docker
            var apiUserEmail = appConfig["ApiUserEmail"] ?? "server@wazalo.com";
            var apiPassword = appConfig["ApiPassword"] ?? "123ABCabc!";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["ApiUserEmail"] = apiUserEmail;
            ViewData["ApiPassword"] = apiPassword;
            ViewData["Word"] = word;

            string token;
            if (HttpContext.Session.GetString("token") == null)
            {
                token = GetJwtToken(apiBaseLocalUrl, apiUserEmail, apiPassword);
                if (token != null) HttpContext.Session.SetString("token", token);
            }
            else
            {
                token = HttpContext.Session.GetString("token");
            }
            ViewData["Token"] = token;

            return View();
        }

        [Route("synonymsbyid/{id}")]
        public IActionResult Index(int id)
        {
            var apiBaseUrl = appConfig["ApiBaseUrl"] ?? "http://localhost:8000/api/";
            var apiBaseLocalUrl = appConfig["ApiBaseLocalUrl"] ?? apiBaseUrl; // to support localhost in docker
            var apiUserEmail = appConfig["ApiUserEmail"] ?? "server@wazalo.com";
            var apiPassword = appConfig["ApiPassword"] ?? "123ABCabc!";

            ViewData["ApiBaseUrl"] = apiBaseUrl;
            ViewData["ApiUserEmail"] = apiUserEmail;
            ViewData["ApiPassword"] = apiPassword;
            ViewData["WordId"] = id;

            string token;
            if (HttpContext.Session.GetString("token") == null)
            {
                token = GetJwtToken(apiBaseLocalUrl, apiUserEmail, apiPassword);
                if (token != null) HttpContext.Session.SetString("token", token);
            }
            else
            {
                token = HttpContext.Session.GetString("token");
            }
            ViewData["Token"] = token;

            return View();
        }

        private string GetJwtToken(string apiBaseUrl, string apiUserEmail, string apiPassword)
        {
            var authUrl = $"{apiBaseUrl}Account/Login";

            dynamic userModel = new JObject();
            userModel.UserName = apiUserEmail;
            userModel.Password = apiPassword;

            string token = string.Empty;
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, authUrl))
                {
                    var stringContent = new StringContent(JsonConvert.SerializeObject(userModel), Encoding.UTF8, "application/json");
                    request.Content = stringContent;

                    using (var response = httpClient.SendAsync(request, System.Threading.CancellationToken.None).Result)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;

                        if (response.IsSuccessStatusCode == true)
                        {
                            dynamic obj = JsonConvert.DeserializeObject<dynamic>(content);
                            token = obj.token;
                        }
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
