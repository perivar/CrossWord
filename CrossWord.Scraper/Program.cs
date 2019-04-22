using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace CrossWord.Scraper
{
    public class Program
    {
        const string DEFAULT_LOG_PATH = "crossword_scraper.log";
        const string DEFAULT_ERROR_LOG_PATH = "crossword_scraper_error.log";

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                        .AddCommandLine(args)
                        .AddEnvironmentVariables()
                        .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                // .MinimumLevel.Debug() // enable ef core logging
                // .MinimumLevel.Information() // disable ef core logging
                // .WriteTo.File(DEFAULT_LOG_PATH)
                // .WriteTo.Console()
                // .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                // .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(DEFAULT_ERROR_LOG_PATH))
                .CreateLogger();

            var signalRHubURL = configuration["SignalRHubURL"] ?? "http://localhost:5000/crosswords";

            // start DOCKER on port 3360
            // docker run -p 3360:3306 --name mysqldb -e MYSQL_ROOT_PASSWORD=password -d mysql:8.0.15            

            // Build database connection string
            var dbhost = configuration["DBHOST"] ?? "localhost";
            var dbport = configuration["DBPORT"] ?? "3306";
            var dbuser = configuration["DBUSER"] ?? "user";
            var dbpassword = configuration["DBPASSWORD"] ?? "password";
            var database = configuration["DATABASE"] ?? "dictionary";

            string connectionString = $"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={database}; charset=utf8;";

            string siteUsername = configuration["kryssord.org:Username"];
            string sitePassword = configuration["kryssord.org:Password"];

            Log.Error("Starting CrossWord.Scraper - retrieving database ....");

            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext(connectionString, Log.Logger))
            {
                // setup database
                // You would either call EnsureCreated() or Migrate(). 
                // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                // db.Database.EnsureDeleted();
                // db.Database.EnsureCreated();

                // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                db.Database.Migrate();
            }

            // make sure that no chrome and chrome drivers are running
            ChromeDriverUtils.KillAllChromeDriverInstances();

            // read inn scraper info from environment variables (docker-compose)
            string scraperSite = configuration["ScraperSite"] ?? "Kryssord";
            bool doContinueWithLastWord = GetConfigurationBoolValue(configuration, "ScraperContinueLastWord", true);
            int startLetterCount = GetConfigurationIntValue(configuration, "ScraperStartLetterCount", 1);
            int endLetterCount = GetConfigurationIntValue(configuration, "ScraperEndLetterCount", 20);

            Log.Error("Using scraper config - site: '{0}', continue with last word: '{1}', from/to letter count: {2}-{3}.", scraperSite, doContinueWithLastWord, startLetterCount, endLetterCount);

            // start several scrapers in parallell
            var options = new ParallelOptions();
            // options.MaxDegreeOfParallelism = 50; // seems to work better without a MaxDegreeOfParallelism number

            // using Parallel.ForEach
            var actionsList = new List<Action>();
            for (int i = startLetterCount; i <= endLetterCount; i++)
            {
                int local_i = i; // have to use local i to not use the same increment on all scrapers
                switch (scraperSite)
                {
                    default:
                    case "Kryssord":
                        actionsList.Add(() => { new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, local_i, doContinueWithLastWord); });

                        break;
                    case "KryssordHjelp":
                        actionsList.Add(() => { new KryssordHjelpScraper(connectionString, signalRHubURL, local_i, doContinueWithLastWord); });
                        break;
                }
            }

            Parallel.ForEach<Action>(actionsList, options, (o => o()));
        }

        private static int GetConfigurationIntValue(IConfiguration configuration, string key, int defaultValue)
        {
            string stringValue = configuration[key] ?? defaultValue.ToString();
            int returnValue = defaultValue;
            int.TryParse(stringValue, out returnValue);
            return returnValue;
        }

        private static bool GetConfigurationBoolValue(IConfiguration configuration, string key, bool defaultValue)
        {
            string stringValue = configuration[key] ?? defaultValue.ToString();
            bool returnValue = defaultValue;
            bool.TryParse(stringValue, out returnValue);
            return returnValue;
        }
    }
}
