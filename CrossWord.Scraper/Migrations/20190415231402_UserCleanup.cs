using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class UserCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "DictionaryUsers");

            migrationBuilder.DropColumn(
                name: "isVIP",
                table: "DictionaryUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "DictionaryUsers",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "isVIP",
                table: "DictionaryUsers",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
