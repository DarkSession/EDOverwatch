using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddRescueShipAllegiance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RescueAllegianceId",
                table: "Station",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Station_RescueAllegianceId",
                table: "Station",
                column: "RescueAllegianceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Station_FactionAllegiance_RescueAllegianceId",
                table: "Station",
                column: "RescueAllegianceId",
                principalTable: "FactionAllegiance",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Station_FactionAllegiance_RescueAllegianceId",
                table: "Station");

            migrationBuilder.DropIndex(
                name: "IX_Station_RescueAllegianceId",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "RescueAllegianceId",
                table: "Station");
        }
    }
}
