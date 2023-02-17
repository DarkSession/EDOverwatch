using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddFactionOperationApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DataUpdate",
                table: "ApiKey",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Faction",
                table: "ApiKey",
                type: "varchar(8)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "FactionUpdate",
                table: "ApiKey",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataUpdate",
                table: "ApiKey");

            migrationBuilder.DropColumn(
                name: "Faction",
                table: "ApiKey");

            migrationBuilder.DropColumn(
                name: "FactionUpdate",
                table: "ApiKey");
        }
    }
}
