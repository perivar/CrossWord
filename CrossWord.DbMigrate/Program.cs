﻿using System;
using System.IO;
using System.Linq;
using CrossWord.DbMigrate.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CrossWord.DbMigrate
{
    class Program
    {
        static Scraper.MySQLDbService.WordHintDbContext CreateDbContext(string connectionString, bool doDebug = false)
        {
            if (doDebug)
            {
                var dbContextFactory = new Scraper.MySQLDbService.DesignTimeDbContextFactory();
                return dbContextFactory.CreateDbContext(connectionString, null);
            }
            else
            {
                var options = new DbContextOptionsBuilder<Scraper.MySQLDbService.WordHintDbContext>();
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                return new Scraper.MySQLDbService.WordHintDbContext(options.Options);
            }
        }

        static WordHintDbContextOrig CreateDbContextOrig(string connectionString, bool doDebug = false)
        {
            if (doDebug)
            {
                var dbContextFactoryOrig = new DesignTimeDbContextFactoryOrig();
                return dbContextFactoryOrig.CreateDbContext(connectionString, null);
            }
            else
            {
                var options = new DbContextOptionsBuilder<WordHintDbContextOrig>();
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                return new WordHintDbContextOrig(options.Options);
            }
        }

        private static bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }

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
                .CreateLogger();

            Log.Information("Starting CrossWord DbMigrate ver. {0} ", "1.0");

            if (InDocker)
            {
                Log.Information("Running in docker container!");
            }

            // read in the start word index
            // e.g. an argument like "STARTWORDINDEX=65000"
            var startWordIndex = configuration.GetValue<int>("STARTWORDINDEX");

            // Setup the two databases
            // Build database connection string
            var dbhost = configuration["DBHOST"] ?? "localhost";
            var dbport = configuration["DBPORT"] ?? "3306";
            var dbuser = configuration["DBUSER"] ?? "user";
            var dbpassword = configuration["DBPASSWORD"] ?? "password";
            var origDatabase = configuration["ORIGDATABASE"] ?? "dictionaryold";
            var newDatabase = configuration["NEWDATABASE"] ?? "dictionary";

            string dbOrigConnectionString = $"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={origDatabase}; charset=utf8;";
            string dbConnectionString = $"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={newDatabase}; charset=utf8;";

            Log.Information("Connection string to original database: {0} ", dbOrigConnectionString);
            Log.Information("Connection string to new database: {0} ", dbConnectionString);

            // SQL debugging?
            const bool doSQLDebug = false;

            // set debugging flag to be able to check whether we are in debug mode in VSCode or in Release mode
            bool isDebugging = false;
#if DEBUG
            isDebugging = true;
#endif

            // last inserted word id
            int lastWordId = 0;

            // setup database
            using (var dbOrig = CreateDbContextOrig(dbOrigConnectionString, doSQLDebug))
            {
                // You would either call EnsureCreated() or Migrate(). 
                // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                // db.Database.EnsureDeleted();
                // db.Database.EnsureCreated();

                // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                dbOrig.Database.Migrate();

                // get last inserted word id
                lastWordId = dbOrig.Words.Max(p => p.WordId);
            }

            // setup database
            using (var db = CreateDbContext(dbConnectionString, doSQLDebug))
            {
                // You would either call EnsureCreated() or Migrate(). 
                // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                // db.Database.EnsureDeleted();
                // db.Database.EnsureCreated();

                // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                db.Database.Migrate();
            }

            // Chunk reading the database
            int takeSize = 1000;
            int loopCounter = startWordIndex != 0 ? startWordIndex / takeSize : 0;
            int skipPos = loopCounter * takeSize;

            while (true)
            {
                // re-open the original context for each main loop
                using (var dbOrig = CreateDbContextOrig(dbOrigConnectionString, doSQLDebug))
                {
                    // disable tracking to speed things up
                    // note that this doesn't load the virtual properties, but loads the object ids after a save
                    dbOrig.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    // this works when using the same user for all words.
                    dbOrig.ChangeTracker.AutoDetectChangesEnabled = false;


                    // re-open the new context for each main loop
                    using (var db = CreateDbContext(dbConnectionString, doSQLDebug))
                    {
                        // Note! 
                        // the user needs to be added before we disable tracking and disable AutoDetectChanges
                        // otherwise this will crash

                        // set admin user
                        var adminUser = new Scraper.MySQLDbService.Models.User()
                        {
                            FirstName = "",
                            LastName = "Admin",
                            UserName = "admin"
                        };

                        // check if user already exists
                        var existingUser = db.DictionaryUsers.Where(u => u.FirstName == adminUser.FirstName).FirstOrDefault();
                        if (existingUser != null)
                        {
                            adminUser = existingUser;
                        }
                        else
                        {
                            db.DictionaryUsers.Add(adminUser);
                            db.SaveChanges();
                        }

                        // disable tracking to speed things up
                        // note that this doesn't load the virtual properties, but loads the object ids after a save
                        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // this works when using the same user for all words.
                        db.ChangeTracker.AutoDetectChangesEnabled = false;


                        // read in from the original database in chunks
                        var origWords = dbOrig.Words
                            .Include(u => u.User)
                            .Include(wh => wh.WordHints)
                            .ThenInclude(h => h.Hint)
                            .ThenInclude(u => u.User)
                            .OrderBy(x => x.WordId)
                            .Skip(skipPos).Take(takeSize)
                            .AsEnumerable();

                        // update chunk parameters
                        skipPos += takeSize;

                        var totalCount = origWords.Count();
                        if (totalCount > 0)
                        {
                            // original word loop
                            int wordCounter = 0;
                            foreach (var origWord in origWords)
                            {
                                if (origWord.WordHints.Count > 0)
                                {
                                    wordCounter++;

                                    // build word object
                                    string wordText = origWord.Value;
                                    var word = new Scraper.MySQLDbService.Models.Word
                                    {
                                        Language = "no",
                                        Value = wordText.ToUpper(),
                                        NumberOfLetters = Scraper.ScraperUtils.CountNumberOfLetters(wordText),
                                        NumberOfWords = Scraper.ScraperUtils.CountNumberOfWords(wordText),
                                        User = adminUser,
                                        CreatedDate = origWord.CreatedDate,
                                        Source = "kryssord.org",
                                        Comment = "User " + origWord.User.ExternalId
                                    };

                                    // get all related words (hints) as Word objects
                                    // ensure related are all uppercase and distinct
                                    var relatedWords = origWord.WordHints.Select(a =>
                                        new Scraper.MySQLDbService.Models.Word
                                        {
                                            Language = "no",
                                            Value = a.Hint.Value.ToUpper(),
                                            NumberOfLetters = Scraper.ScraperUtils.CountNumberOfLetters(a.Hint.Value),
                                            NumberOfWords = Scraper.ScraperUtils.CountNumberOfWords(a.Hint.Value),
                                            User = adminUser,
                                            CreatedDate = a.Hint.CreatedDate,
                                            Source = "kryssord.org",
                                            Comment = "User " + a.Hint.User.ExternalId
                                        }
                                    ).Distinct(); // Note that this requires the object to implement IEquatable<Word> 

                                    // add to database
                                    Scraper.MySQLDbService.WordDatabaseService.AddToDatabase(db, "kryssord.org", word, relatedWords);

                                    if (isDebugging)
                                    {
                                        // in debug mode the Console.Write \r isn't shown in the output console
                                        if ((wordCounter % 10) == 0) Console.WriteLine("[{0}] / [{1}]", wordCounter + (loopCounter * takeSize), lastWordId);
                                    }
                                    else
                                    {
                                        if (InDocker)
                                        {
                                            if ((wordCounter % 100) == 0) Log.Information("[{0}] / [{1}]", wordCounter + (loopCounter * takeSize), lastWordId);
                                        }
                                        else
                                        {
                                            if ((wordCounter % 10) == 0) Console.Write("\r[{0}] / [{1}]", wordCounter + (loopCounter * takeSize), lastWordId);
                                        }
                                    }
                                }
                            }

                            loopCounter++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            Console.WriteLine();
            Log.Information("CrossWord DbMigrate Completed");
        }
    }
}