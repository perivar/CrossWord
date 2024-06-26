using System;
using CrossWord.DbMigrate.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.DbMigrate.MySQLDbService
{
    public class WordHintDbContextOrig : IdentityDbContext
    {
        public DbSet<Word> Words { get; set; }
        public DbSet<Hint> Hints { get; set; }
        public DbSet<User> DictionaryUsers { get; set; }

        public WordHintDbContextOrig()
            : base()
        {
        }

        public WordHintDbContextOrig(DbContextOptions<WordHintDbContextOrig> options)
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

            modelBuilder.Entity<WordHint>()
                        .HasKey(wh => new { wh.WordId, wh.HintId });

            modelBuilder.Entity<WordHint>()
                        .HasOne(wh => wh.Word)
                        .WithMany(w => w.WordHints)
                        .HasForeignKey(wh => wh.WordId);

            modelBuilder.Entity<WordHint>()
                        .HasOne(wh => wh.Hint)
                        .WithMany(h => h.WordHints)
                        .HasForeignKey(wh => wh.HintId);

            // have to manually ensure bools are converted to 1 and 0 due to a bug in the mysql driver
            modelBuilder.Entity<User>()
                        .Property(u => u.isVIP)
                        .HasConversion(new BoolToZeroOneConverter<Int16>());

            modelBuilder.Entity<Word>()
                        .Property(w => w.Value)
                        .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // only works with the Pomelo driver if overriding MySqlMigrationsSqlGenerator
            // .ForMySQLHasCollation("utf8mb4_0900_as_cs"); // defining collation in a property as accent sensitive (as) and case sensitive (cs)

            modelBuilder.Entity<Hint>()
                        .Property(h => h.Value)
                        .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // only works with the Pomelo driver if overriding MySqlMigrationsSqlGenerator
            // .ForMySQLHasCollation("utf8mb4_0900_as_cs"); // defining collation in a property as accent sensitive (as) and case sensitive (cs)
        }
    }
}