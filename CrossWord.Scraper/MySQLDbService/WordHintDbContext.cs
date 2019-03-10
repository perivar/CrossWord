using System;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossWord.Scraper.MySQLDbService
{
    public class WordHintDbContext : DbContext
    {
        public DbSet<Word> Words { get; set; }
        public DbSet<Hint> Hints { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=dictionary;user=user;password=password");
            base.OnConfiguring(optionsBuilder);
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
        }
    }
}