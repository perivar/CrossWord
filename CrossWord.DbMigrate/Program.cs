using System;
using System.Linq;
using CrossWord.DbMigrate.MySQLDbService;
using Microsoft.EntityFrameworkCore;

namespace CrossWord.DbMigrate
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbContextFactory = new DesignTimeDbContextFactory();
            using (var db = dbContextFactory.CreateDbContext("server=localhost;database=dictionaryold;user=user;password=password;charset=utf8;", null)) // null instead of Log.Logger enables debugging
            {
                // setup database
                // You would either call EnsureCreated() or Migrate(). 
                // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
                // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
                // db.Database.EnsureDeleted();
                // db.Database.EnsureCreated();

                // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
                db.Database.Migrate();

                var words = db.Words.Where(w => w.Value == "FEIT")
                    .Include(u => u.User)
                    .Include(wh => wh.WordHints)
                    .ThenInclude(h => h.Hint);

                // var words = db.Words
                //     .Include(u => u.User)
                //     .Include(wh => wh.WordHints)
                //     .ThenInclude(h => h.Hint);

                foreach (var word in words)
                {
                    Console.WriteLine("Found {0} with {1} hints:", word, word.WordHints.Count);
                    foreach (var hint in word.WordHints)
                    {
                        Console.WriteLine("{0}", hint.Hint.Value);
                    }
                }
            }
        }
    }
}
