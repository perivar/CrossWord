using System;
using System.Linq;
using CrossWord.DbMigrate.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CrossWord.DbMigrate
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CrossWord DbMigrate ver. {0} ", "1.0");

            var dbContextFactoryOrig = new DesignTimeDbContextFactoryOrig();
            using (var dbOrig = dbContextFactoryOrig.CreateDbContext("server=localhost;database=dictionaryold;user=user;password=password;charset=utf8;", Log.Logger)) // null instead of Log.Logger enables debugging
            {
                // setup database
                // You would either call EnsureCreated() or Migrate(). 
                // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                // db.Database.EnsureDeleted();
                // db.Database.EnsureCreated();

                // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                dbOrig.Database.Migrate();

                // disable tracking to speed things up
                // note that this doesn't load the virtual properties, but loads the object ids after a save
                dbOrig.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                // this works when using the same user for all words.
                dbOrig.ChangeTracker.AutoDetectChangesEnabled = false;

                var dbContextFactory = new Scraper.MySQLDbService.DesignTimeDbContextFactory();
                using (var db = dbContextFactory.CreateDbContext("server=localhost;database=dictionary;user=user;password=password;charset=utf8;", Log.Logger)) // null instead of Log.Logger enables debugging
                {
                    // setup database
                    // You would either call EnsureCreated() or Migrate(). 
                    // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                    // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                    // db.Database.EnsureDeleted();
                    // db.Database.EnsureCreated();

                    // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                    db.Database.Migrate();

                    // set admin user
                    var user = new Scraper.MySQLDbService.Models.User()
                    {
                        FirstName = "Admin",
                        LastName = "Admin",
                        UserName = "",
                        isVIP = true
                    };

                    // check if user already exists
                    var existingUser = db.DictionaryUsers.Where(u => u.FirstName == user.FirstName).FirstOrDefault();
                    if (existingUser != null)
                    {
                        user = existingUser;
                    }
                    else
                    {
                        db.DictionaryUsers.Add(user);
                        db.SaveChanges();
                    }

                    // disable tracking to speed things up
                    // note that this doesn't load the virtual properties, but loads the object ids after a save
                    db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    // this works when using the same user for all words.
                    db.ChangeTracker.AutoDetectChangesEnabled = false;

                    bool isDebugging = false;
#if DEBUG
                    isDebugging = true;
#endif                    

                    // read in from the original database
                    var words = dbOrig.Words
                        .Include(u => u.User)
                        .Include(wh => wh.WordHints)
                        .ThenInclude(h => h.Hint);

                    var totalCount = words.Count();
                    int count = 0;
                    foreach (var word in words)
                    {
                        if (word.WordHints.Count > 0)
                        {
                            count++;

                            var relatedWords = word.WordHints.Select(a => a.Hint.Value).ToList();
                            Scraper.MySQLDbService.WordDatabaseService.AddToDatabase(db, user, word.Value, relatedWords);

                            if (isDebugging)
                            {
                                // in debug mode the Console.Write \r isn't shown in the output console
                                Console.WriteLine("[{0}] / [{1}]", count, totalCount);
                            }
                            else
                            {
                                Console.Write("\r[{0}] / [{1}]", count, totalCount);
                            }
                        }
                    }
                }
            }
        }
    }
}