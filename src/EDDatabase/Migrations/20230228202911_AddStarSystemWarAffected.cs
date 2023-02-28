using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddStarSystemWarAffected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WarAffected",
                table: "StarSystem",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_WarAffected",
                table: "StarSystem",
                column: "WarAffected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StarSystem_WarAffected",
                table: "StarSystem");

            migrationBuilder.DropColumn(
                name: "WarAffected",
                table: "StarSystem");
        }
    }
}
