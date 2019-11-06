using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class RefreshTokenWithUserAgent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens");
        }
    }
}
