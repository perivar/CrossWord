using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.DbMigrate.Migrations
{
    public partial class RenamedUserList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hints_Users_UserId",
                table: "Hints");

            migrationBuilder.DropForeignKey(
                name: "FK_Words_Users_UserId",
                table: "Words");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "DictionaryUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DictionaryUsers",
                table: "DictionaryUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hints_DictionaryUsers_UserId",
                table: "Hints",
                column: "UserId",
                principalTable: "DictionaryUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Words_DictionaryUsers_UserId",
                table: "Words",
                column: "UserId",
                principalTable: "DictionaryUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hints_DictionaryUsers_UserId",
                table: "Hints");

            migrationBuilder.DropForeignKey(
                name: "FK_Words_DictionaryUsers_UserId",
                table: "Words");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DictionaryUsers",
                table: "DictionaryUsers");

            migrationBuilder.RenameTable(
                name: "DictionaryUsers",
                newName: "Users");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hints_Users_UserId",
                table: "Hints",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Words_Users_UserId",
                table: "Words",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
