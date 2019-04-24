using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class StateChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_States_Words_WordId",
                table: "States");

            migrationBuilder.DropIndex(
                name: "IX_States_WordId",
                table: "States");

            migrationBuilder.RenameColumn(
                name: "WordId",
                table: "States",
                newName: "NumberOfLetters");

            migrationBuilder.AddColumn<string>(
                name: "Word",
                table: "States",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Word",
                table: "States");

            migrationBuilder.RenameColumn(
                name: "NumberOfLetters",
                table: "States",
                newName: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_States_WordId",
                table: "States",
                column: "WordId");

            migrationBuilder.AddForeignKey(
                name: "FK_States_Words_WordId",
                table: "States",
                column: "WordId",
                principalTable: "Words",
                principalColumn: "WordId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
