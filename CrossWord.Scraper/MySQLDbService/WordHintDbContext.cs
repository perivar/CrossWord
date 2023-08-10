using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using CrossWord.Scraper.MySQLDbService.Models;
using CrossWord.Scraper.MySQLDbService.Entities;
using Serilog;

namespace CrossWord.Scraper.MySQLDbService
{
    public class WordHintDbContext : IdentityDbContext<ApplicationUser>
    {
        private ILogger logger;

        public DbSet<Word> Words { get; set; }
        public DbSet<WordRelation> WordRelations { get; set; }
        public DbSet<User> DictionaryUsers { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CrosswordTemplate> CrosswordTemplates { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        private void InitLoggerIfNull()
        {
            if (logger == null) {
                logger = Log.ForContext<WordHintDbContext>();
            }
        }

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
            InitLoggerIfNull();

            if (logger != null) logger.Information("OnConfiguring()");
            base.OnConfiguring(optionsBuilder);

            if (logger != null) logger.Information("Replacing built-in generator with CustomMySqlMigrationsSqlGenerator");
            optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMySqlMigrationsSqlGenerator>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            InitLoggerIfNull();

            if (logger != null) logger.Information("OnModelCreating()");
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WordRelation>()
                        .HasKey(wh => new { wh.WordFromId, wh.WordToId });

            modelBuilder.Entity<WordRelation>()
                        .HasOne(wh => wh.WordFrom)
                        .WithMany(w => w.RelatedFrom)
                        .HasForeignKey(wh => wh.WordFromId)
                        .OnDelete(DeleteBehavior.Restrict);
            // https://stackoverflow.com/questions/49214748/many-to-many-self-referencing-relationship
            // Note that you have to turn the delete cascade off for at least one of the relationships and manually delete the related join entities before deleting the main entity, 
            // because self referencing relationships always introduce possible cycles or multiple cascade path issue, preventing the usage of cascade delete.

            modelBuilder.Entity<WordRelation>()
                        .HasOne(wh => wh.WordTo)
                        .WithMany(w => w.RelatedTo) // Unidirectional Many-to-Many Relationship has no reverse mapping but using bi-directional because the include only includes the first mapping
                        .HasForeignKey(wh => wh.WordToId);

            // ensure the value field is unique
            // Note! this screws up the sql generations for the collation - see below. 
            // See Generate(AlterColumnOperation alterColumnOperation, IModel model, MigrationCommandListBuilder builder) in CustomMySqlMigrationsSqlGenerator            
            modelBuilder.Entity<Word>()
                        .HasIndex(w => w.Value)
                        .IsUnique();

            // ensure the value field is accent sensitive and case sensitive
            modelBuilder.Entity<Word>()
                        .Property(w => w.Value)
                        // .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // Note! this only works with the Pomelo driver if overriding ColumnDefinition in MySqlMigrationsSqlGenerator
                        .UseCollation("utf8mb4_0900_as_cs");

            // ensure the Source field is accent sensitive and case sensitive
            // Note! this didn't actually do anything - had to add the collation manually in StatesCollation -> Up(MigrationBuilder migrationBuilder)
            modelBuilder.Entity<State>()
                        .Property(w => w.Word)
                        // .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // Note! this only works with the Pomelo driver if overriding ColumnDefinition in MySqlMigrationsSqlGenerator
                        .UseCollation("utf8mb4_0900_as_cs");

            modelBuilder.Entity<State>()
                        .Property(w => w.Comment)
                        // .HasAnnotation("MySql:Collation", "utf8mb4_0900_as_cs"); // Note! this only works with the Pomelo driver if overriding ColumnDefinition in MySqlMigrationsSqlGenerator
                        .UseCollation("utf8mb4_0900_as_cs");

            // Save array of string in EntityFramework Core by using a private field to store the array as a string
            modelBuilder.Entity<CrosswordTemplate>()
                        .Property<string>("_grid") // Name of field
                        .UsePropertyAccessMode(PropertyAccessMode.Field) // Access mode type
                        .HasColumnName("GridCollection"); // Db column name

            // each User can have many entries in the RefreshTokens table
            modelBuilder.Entity<ApplicationUser>()
                        .HasMany(e => e.RefreshTokens)
                        .WithOne(e => e.ApplicationUser)
                        .HasForeignKey(e => e.ApplicationUserId)
                        .IsRequired();
        }
    }
}