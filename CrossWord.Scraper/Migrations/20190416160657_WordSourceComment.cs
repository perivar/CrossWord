using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class WordSourceComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Words",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Words",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Words");
        }
    }
}
