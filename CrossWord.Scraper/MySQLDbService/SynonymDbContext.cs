using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossWord.Scraper.MySQLDbService
{
    public class SynonymDbContext : DbContext
    {
        public DbSet<Word> Words { get; set; }

        // https://stackoverflow.com/questions/51308245/ef-core-2-1-self-referencing-entity-with-one-to-many-relationship-generates-add
        // https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core-example.html
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=dictionary;user=user;password=password");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Word>()
                         .HasOne(x => x.Parent)
                         .WithMany(x => x.Synonyms)
                         .HasForeignKey(x => x.ParentWordId);

        }
    }
}