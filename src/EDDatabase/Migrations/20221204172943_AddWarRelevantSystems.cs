using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddWarRelevantSystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WarRelevantSystem",
                table: "StarSystem",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_State",
                table: "StarSystemThargoidLevel",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_WarRelevantSystem",
                table: "StarSystem",
                column: "WarRelevantSystem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StarSystemThargoidLevel_State",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropIndex(
                name: "IX_StarSystem_WarRelevantSystem",
                table: "StarSystem");

            migrationBuilder.DropColumn(
                name: "WarRelevantSystem",
                table: "StarSystem");
        }
    }
}
