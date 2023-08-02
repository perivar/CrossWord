using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CrossWord.Scraper.MySQLDbService
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WordHintDbContext>
    {
        const string CONNECTION_STRING_KEY = "DefaultConnection";

        public WordHintDbContext CreateDbContext()
        {
            return CreateDbContext(Array.Empty<string>());
        }

        public WordHintDbContext CreateDbContext(string[] args)
        {
            return CreateDbContext(args, null);
        }

        public WordHintDbContext CreateDbContext(string connectionString)
        {
            return CreateDbContext(connectionString, null);
        }

        public WordHintDbContext CreateDbContext(string connectionString, Serilog.ILogger log)
        {
            var args = new string[] { $"ConnectionStrings:DefaultConnection={connectionString}" };
            return CreateDbContext(args, log);
        }

        public WordHintDbContext CreateDbContext(string[] args, Serilog.ILogger log)
        {
            // set logging
            ILoggerFactory loggerFactory = new LoggerFactory();

            // this is only null when called from 'dotnet ef migrations ...'
            log ??= new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .WriteTo.Console()
                    .CreateLogger();

            var options = new DbContextOptionsBuilder<WordHintDbContext>();

            // since Entity Framework outputs so much information at even Information level
            // only output to serilog if log level is debug or lower
            if (log.IsEnabled(LogEventLevel.Debug) || log.IsEnabled(LogEventLevel.Verbose))
            {
                // Disable client evaluation in development environment
                options.UseSerilog(loggerFactory, throwOnQueryWarnings: true);

                // add this line to output Entity Framework log statements
                loggerFactory.AddSerilog(log);
            }

            string connectionString = GetConnectionString(args);
            Log.Information($"Using connection string: {connectionString}");

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)); // default added as Scoped

            return new WordHintDbContext(options.Options);
        }

        /// <summary>
        /// Read connection string from passed arguments or appsettings.json
        /// </summary>
        /// <param name="args">arguments</param>
        /// <example>var args = new string[] { $"ConnectionStrings:DefaultConnection=server=localhost;database=dictionary;user=user;password=password;charset=utf8;" };</example>
        private static string GetConnectionString(string[] args)
        {
            Dictionary<string, string> inMemoryCollection = new();

            if (args.Any())
            {
                // Connection strings has keys like "ConnectionStrings:DefaultConnection" 
                // and values like "Data Source=C:\\Users\\pnerseth\\My Projects\\fingerprint.db" for Sqlite
                // or
                // server=localhost;port=3360;database=dictionary;user=root;password=secret;charset=utf8; for Mysql
                Log.Information($"Searching for '{CONNECTION_STRING_KEY}' within passed arguments: {string.Join(", ", args)}");
                var match = args.FirstOrDefault(s => s.Contains($"ConnectionStrings:{CONNECTION_STRING_KEY}"));
                if (match != null)
                {
                    Regex pattern = new($"(?<name>ConnectionStrings:{CONNECTION_STRING_KEY})=(?<value>.+?)$");

                    inMemoryCollection = Enumerable.ToDictionary(
                      Enumerable.Cast<Match>(pattern.Matches(match)),
                      m => m.Groups["name"].Value,
                      m => m.Groups["value"].Value);
                }
            }
            else
            {
                Log.Information($"Searching for '{CONNECTION_STRING_KEY}' in {Directory.GetCurrentDirectory()} => appsettings(.Development).json");
            }

            var configurationBuilder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                        .AddInMemoryCollection(inMemoryCollection);

            IConfigurationRoot configuration = configurationBuilder.Build();

            return configuration.GetConnectionString(CONNECTION_STRING_KEY);
        }
    }
}