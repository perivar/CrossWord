using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false)
                        .Annotation("MySQL:AutoIncrement", true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    isVIP = table.Column<short>(nullable: false),
                    ExternalId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Hints",
                columns: table => new
                {
                    HintId = table.Column<int>(nullable: false)
                        .Annotation("MySQL:AutoIncrement", true),
                    Language = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    NumberOfLetters = table.Column<int>(nullable: false),
                    NumberOfWords = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hints", x => x.HintId);
                    table.ForeignKey(
                        name: "FK_Hints_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    WordId = table.Column<int>(nullable: false)
                        .Annotation("MySQL:AutoIncrement", true),
                    Language = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    NumberOfLetters = table.Column<int>(nullable: false),
                    NumberOfWords = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.WordId);
                    table.ForeignKey(
                        name: "FK_Words_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WordHint",
                columns: table => new
                {
                    WordId = table.Column<int>(nullable: false),
                    HintId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordHint", x => new { x.WordId, x.HintId });
                    table.ForeignKey(
                        name: "FK_WordHint_Hints_HintId",
                        column: x => x.HintId,
                        principalTable: "Hints",
                        principalColumn: "HintId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordHint_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hints_UserId",
                table: "Hints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordHint_HintId",
                table: "WordHint",
                column: "HintId");

            migrationBuilder.CreateIndex(
                name: "IX_Words_UserId",
                table: "Words",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordHint");

            migrationBuilder.DropTable(
                name: "Hints");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
