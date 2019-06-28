using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class WordRelationDateAndComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "WordRelations",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "WordRelations",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "WordRelations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "WordRelations");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "WordRelations");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "WordRelations");
        }
    }
}
