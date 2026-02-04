using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logos.AI.Engine.Migrations
{
    public partial class AddDocumentTitleAndDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentTitle",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentDescription",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentTitle",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentDescription",
                table: "Documents");
        }
    }
}