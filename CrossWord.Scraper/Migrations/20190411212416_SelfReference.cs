using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class SelfReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordHint");

            migrationBuilder.DropTable(
                name: "Hints");

            migrationBuilder.CreateTable(
                name: "WordRelations",
                columns: table => new
                {
                    WordFromId = table.Column<int>(nullable: false),
                    WordToId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordRelations", x => new { x.WordFromId, x.WordToId });
                    table.ForeignKey(
                        name: "FK_WordRelations_Words_WordFromId",
                        column: x => x.WordFromId,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WordRelations_Words_WordToId",
                        column: x => x.WordToId,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WordRelations_WordToId",
                table: "WordRelations",
                column: "WordToId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordRelations");

            migrationBuilder.CreateTable(
                name: "Hints",
                columns: table => new
                {
                    HintId = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    Language = table.Column<string>(nullable: true),
                    NumberOfLetters = table.Column<int>(nullable: false),
                    NumberOfWords = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hints", x => x.HintId);
                    table.ForeignKey(
                        name: "FK_Hints_DictionaryUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DictionaryUsers",
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
        }
    }
}
