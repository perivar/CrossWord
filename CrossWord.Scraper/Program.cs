using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CrossWord.Scraper.Extensions;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CrossWord.Scraper
{
    public class Program
    {
        // const string DEFAULT_LOG_PATH = "crossword_scraper.log";
        // const string DEFAULT_ERROR_LOG_PATH = "crossword_scraper_error.log";

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

            var signalRHubURL = configuration["SignalRHubURL"] ?? "http://localhost:8000/crosswordsignalrhub";

            // either use a local mysql install running on port 3306 (default)
            // or use Docker:
            // spinning up directly on port 3360
            // docker run -p 3360:3306 --name mysqldb -e MYSQL_ROOT_PASSWORD=password -d mysql:8.0.15            
            // or use docker compose to init with data on port 3360
            // docker compose up db -d
            // and enter using
            // docker exec -it crossword.db mysql -uroot -psecret dictionary

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
            bool doContinueWithLastWord = configuration.GetBoolValue("ScraperContinueLastWord", true);
            int startLetterCount = configuration.GetIntValue("ScraperStartLetterCount", 1);
            int endLetterCount = configuration.GetIntValue("ScraperEndLetterCount", 20);
            bool isScraperSwarm = configuration.GetBoolValue("ScraperSwarm", true);
            bool isKryssordLatest = configuration.GetBoolValue("KryssordLatest", false);
            int kryssordLatestDelaySeconds = configuration.GetIntValue("KryssordLatestDelaySeconds", 60);

            Log.Error("Using scraper config - site: '{0}', continue with last word: '{1}', from/to letter count: {2}-{3}. Swarming: {4}", scraperSite, doContinueWithLastWord, startLetterCount, endLetterCount, isScraperSwarm);

            // start several scrapers in parallell
            var options = new ParallelOptions();
            // options.MaxDegreeOfParallelism = 50; // seems to work better without a MaxDegreeOfParallelism number

#if DEBUG
            startLetterCount = 4;
            endLetterCount = 8;
            scraperSite = "Kryssord";
            isScraperSwarm = false;
            isKryssordLatest = true;
            kryssordLatestDelaySeconds = 30;
            doContinueWithLastWord = false;
#endif

            if (isScraperSwarm)
            {
                // using Parallel.ForEach
                var actionsList = new List<Action>();
                for (int i = startLetterCount; i <= endLetterCount; i++)
                {
                    int local_i = i; // have to use local i to not use the same increment on all scrapers
                    switch (scraperSite)
                    {
                        default:
                        case "Kryssord":
                            actionsList.Add(() => { new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, local_i, endLetterCount, doContinueWithLastWord, isScraperSwarm); });
                            break;
                        case "KryssordHjelp":
                            actionsList.Add(() => { new KryssordHjelpScraper(connectionString, signalRHubURL, local_i, doContinueWithLastWord); });
                            break;
                        case "GratisKryssord":
                            actionsList.Add(() => { new GratisKryssordScraper(connectionString, signalRHubURL, local_i, endLetterCount, doContinueWithLastWord); });
                            break;
                        case "NorwegianSynonyms":
                            actionsList.Add(() => { new NorwegianSynonymsScraper(connectionString, signalRHubURL, local_i, endLetterCount, doContinueWithLastWord); });
                            break;
                    }
                }

                // check if we should add a separate thread for kryssord.org latest
                if (isKryssordLatest)
                {
                    Log.Error("Adding a separate swarm thread for kryssord.org latest");
                    actionsList.Add(() => { new KryssordScraperLatest(connectionString, signalRHubURL, siteUsername, sitePassword, kryssordLatestDelaySeconds); });
                }

                Parallel.ForEach(actionsList, options, o => o());
            }
            else
            {
                // check if we should add a separate thread for kryssord.org latest
                if (isKryssordLatest)
                {
                    Log.Error("Running kryssord.org latest");
                    new KryssordScraperLatest(connectionString, signalRHubURL, siteUsername, sitePassword, kryssordLatestDelaySeconds);
                }
                else
                {
                    // run only one thread
                    switch (scraperSite)
                    {
                        default:
                        case "Kryssord":
                            new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, startLetterCount, endLetterCount, doContinueWithLastWord, false);
                            break;
                        case "KryssordHjelp":
                            new KryssordHjelpScraper(connectionString, signalRHubURL, startLetterCount, doContinueWithLastWord);
                            break;
                        case "GratisKryssord":
                            new GratisKryssordScraper(connectionString, signalRHubURL, startLetterCount, endLetterCount, doContinueWithLastWord);
                            break;
                        case "NorwegianSynonyms":
                            new NorwegianSynonymsScraper(connectionString, signalRHubURL, startLetterCount, endLetterCount, doContinueWithLastWord);
                            break;
                    }
                }
            }
        }
    }
}
