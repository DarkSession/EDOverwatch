using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemCounterstrike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCounterstrike",
                table: "StarSystemThargoidLevel",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "Permissions",
                table: "Commander",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCounterstrike",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Commander");
        }
    }
}
