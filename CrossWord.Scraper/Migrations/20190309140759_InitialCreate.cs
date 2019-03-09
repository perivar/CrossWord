using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    WordId = table.Column<int>(nullable: false)
                        .Annotation("MySQL:AutoIncrement", true),
                    ParentWordId = table.Column<int>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    NumberOfLetters = table.Column<int>(nullable: false),
                    NumberOfWords = table.Column<int>(nullable: false),
                    UserId = table.Column<long>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.WordId);
                    table.ForeignKey(
                        name: "FK_Words_Words_ParentWordId",
                        column: x => x.ParentWordId,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Words_ParentWordId",
                table: "Words",
                column: "ParentWordId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Words");
        }
    }
}
