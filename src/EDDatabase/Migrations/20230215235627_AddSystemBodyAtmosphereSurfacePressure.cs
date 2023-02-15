using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemBodyAtmosphereSurfacePressure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAtmosphere",
                table: "StarSystemBody",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SurfacePressure",
                table: "StarSystemBody",
                type: "decimal(18,8)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAtmosphere",
                table: "StarSystemBody");

            migrationBuilder.DropColumn(
                name: "SurfacePressure",
                table: "StarSystemBody");
        }
    }
}
