﻿// <auto-generated />
using System;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrossWord.Scraper.Migrations
{
    [DbContext(typeof(WordHintDbContext))]
    [Migration("20190310100556_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.Hint", b =>
                {
                    b.Property<int>("HintId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedDate");

                    b.Property<string>("Language");

                    b.Property<int>("NumberOfLetters");

                    b.Property<int>("NumberOfWords");

                    b.Property<int?>("UserId");

                    b.Property<string>("Value");

                    b.HasKey("HintId");

                    b.HasIndex("UserId");

                    b.ToTable("Hints");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<string>("Password");

                    b.Property<string>("UserName");

                    b.Property<short>("isVIP");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.Word", b =>
                {
                    b.Property<int>("WordId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedDate");

                    b.Property<string>("Language");

                    b.Property<int>("NumberOfLetters");

                    b.Property<int>("NumberOfWords");

                    b.Property<int?>("UserId");

                    b.Property<string>("Value");

                    b.HasKey("WordId");

                    b.HasIndex("UserId");

                    b.ToTable("Words");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.WordHint", b =>
                {
                    b.Property<int>("WordId");

                    b.Property<int>("HintId");

                    b.HasKey("WordId", "HintId");

                    b.HasIndex("HintId");

                    b.ToTable("WordHint");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.Hint", b =>
                {
                    b.HasOne("CrossWord.Scraper.MySQLDbService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.Word", b =>
                {
                    b.HasOne("CrossWord.Scraper.MySQLDbService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("CrossWord.Scraper.MySQLDbService.Models.WordHint", b =>
                {
                    b.HasOne("CrossWord.Scraper.MySQLDbService.Models.Hint", "Hint")
                        .WithMany("WordHints")
                        .HasForeignKey("HintId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("CrossWord.Scraper.MySQLDbService.Models.Word", "Word")
                        .WithMany("WordHints")
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
