using System;
using System.Linq;
using CrossWord.DbMigrate.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CrossWord.DbMigrate
{
    class Program
    {
        static Scraper.MySQLDbService.WordHintDbContext CreateDbContext(string dbConnectionString, bool doDebug = false)
        {
            if (doDebug)
            {
                var dbContextFactory = new Scraper.MySQLDbService.DesignTimeDbContextFactory();
                return dbContextFactory.CreateDbContext(dbConnectionString, null);
            }
            else
            {
                var options = new DbContextOptionsBuilder<Scraper.MySQLDbService.WordHintDbContext>();
                options.UseMySql(dbConnectionString);
                return new Scraper.MySQLDbService.WordHintDbContext(options.Options);
            }
        }

        static WordHintDbContextOrig CreateDbContextOrig(string dbOrigConnectionString, bool doDebug = false)
        {
            if (doDebug)
            {
                var dbContextFactoryOrig = new DesignTimeDbContextFactoryOrig();
                return dbContextFactoryOrig.CreateDbContext(dbOrigConnectionString, null);
            }
            else
            {
                var options = new DbContextOptionsBuilder<WordHintDbContextOrig>();
                options.UseMySql(dbOrigConnectionString);
                return new WordHintDbContextOrig(options.Options);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("CrossWord DbMigrate ver. {0} ", "1.0");

            // Setup the two databases
            const string dbConnectionString = "server=localhost;database=dictionary;user=user;password=password;charset=utf8;";
            const string dbOrigConnectionString = "server=localhost;database=dictionaryold;user=user;password=password;charset=utf8;";

            // SQL debugging?
            const bool doSQLDebug = false;

            // set debugging flag to be able to check whether we are in debug mode in VSCode or in Release mode
            bool isDebugging = false;
#if DEBUG
            isDebugging = true;
#endif

            // set admin user
            var adminUser = new Scraper.MySQLDbService.Models.User()
            {
                FirstName = "",
                LastName = "Admin",
                UserName = "admin"
            };

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
            }

            // Chunk reading the database
            int takeSize = 1000;
            int skipPos = 0;
            int loopCounter = 0;

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
                        // disable tracking to speed things up
                        // note that this doesn't load the virtual properties, but loads the object ids after a save
                        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // this works when using the same user for all words.
                        db.ChangeTracker.AutoDetectChangesEnabled = false;


                        // read in from the original database
                        var words = dbOrig.Words
                            .Include(u => u.User)
                            .Include(wh => wh.WordHints)
                            .ThenInclude(h => h.Hint)
                            .OrderBy(x => x.WordId)
                            .Skip(skipPos).Take(takeSize)
                            .AsEnumerable();

                        // update chunk parameters
                        skipPos += takeSize;

                        var totalCount = words.Count();
                        if (totalCount > 0)
                        {
                            // word loop
                            int count = 0;
                            foreach (var word in words)
                            {
                                if (word.WordHints.Count > 0)
                                {
                                    count++;

                                    var relatedWords = word.WordHints.Select(a => a.Hint.Value).ToList();

                                    // add to database
                                    Scraper.MySQLDbService.WordDatabaseService.AddToDatabase(db, adminUser, word.Value, relatedWords);

                                    if (isDebugging)
                                    {
                                        // in debug mode the Console.Write \r isn't shown in the output console
                                        Console.WriteLine("[{0}] / [{1}]", count + (loopCounter * takeSize), lastWordId);
                                    }
                                    else
                                    {
                                        Console.Write("\r[{0}] / [{1}]", count + (loopCounter * takeSize), lastWordId);
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
        }
    }
}