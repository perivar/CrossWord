using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossWord.Scraper.Migrations
{
    public partial class StatesCollations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // add the sql manually to force updating the collation on some States columns 
            // ALTER TABLE <table_name> MODIFY <column_name> VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

            // States - > Comment columns
            migrationBuilder.Sql($@"ALTER TABLE States MODIFY Comment VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs DEFAULT NULL;");

            // States - > Word columns
            migrationBuilder.Sql($@"ALTER TABLE States MODIFY Word VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs DEFAULT NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
