using System;
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

            // start several scrapers in parallell
            var options = new ParallelOptions();
            // // options.MaxDegreeOfParallelism = 50;

            Parallel.Invoke(options,
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa???"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa?????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa??????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa???????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa????????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa?????????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa??????????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa???????????"),
              () => new KryssordScraper(connectionString, signalRHubURL, siteUsername, sitePassword, "aaa????????????")

            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 1),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 2),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 3),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 4),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 5),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 6),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 7),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 8),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 9),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 10),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 11),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 12),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 13),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 14),
            //   () => new KryssordHjelpScraper(connectionString, signalRHubURL, 15)

            );
        }
    }
}
