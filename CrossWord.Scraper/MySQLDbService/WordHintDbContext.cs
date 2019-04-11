using System;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.MySQLDbService
{
    public class WordHintDbContext : IdentityDbContext
    {
        public DbSet<Word> Words { get; set; }
        public DbSet<WordRelation> WordRelations { get; set; }
        public DbSet<User> DictionaryUsers { get; set; }

        public WordHintDbContext()
            : base()
        {
        }

        public WordHintDbContext(DbContextOptions<WordHintDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMySqlMigrationsSqlGenerator>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WordRelation>()
                        .HasKey(wh => new { wh.WordFromId, wh.WordToId });

            modelBuilder.Entity<WordRelation>()
                        .HasOne(wh => wh.WordFrom)
                        .WithMany(w => w.RelatedTo)
                        .HasForeignKey(wh => wh.WordFromId)
                        .OnDelete(DeleteBehavior.Restrict);
            // https://stackoverflow.com/questions/49214748/many-to-many-self-referencing-relationship
            // Note that you have to turn the delete cascade off for at least one of the relationships and manually delete the related join entities before deleting the main entity, 
            // because self referencing relationships always introduce possible cycles or multiple cascade path issue, preventing the usage of cascade delete.

            modelBuilder.Entity<WordRelation>()
                        .HasOne(wh => wh.WordTo)
                        .WithMany(h => h.RelatedFrom)
                        .HasForeignKey(wh => wh.WordToId);

            // have to manually ensure bools are converted to 1 and 0 due to a bug in the mysql driver
            modelBuilder.Entity<User>()
                        .Property(u => u.isVIP)
                        .HasConversion(new BoolToZeroOneConverter<Int16>());

            modelBuilder.Entity<Word>()
                        .Property(w => w.Value)
                        .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // only works with the Pomelo driver if overriding MySqlMigrationsSqlGenerator
        }
    }
}