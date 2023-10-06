using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class IndexImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstrom_Name",
                table: "ThargoidMaelstrom",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Station_Name",
                table: "Station",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThargoidMaelstrom_Name",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropIndex(
                name: "IX_Station_Name",
                table: "Station");
        }
    }
}
