using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalMaelstromData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InfluenceSphere",
                table: "ThargoidMaelstrom",
                type: "decimal(14,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaelstromId",
                table: "StarSystemThargoidLevel",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_MaelstromId",
                table: "StarSystemThargoidLevel",
                column: "MaelstromId");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidMaelstrom_MaelstromId",
                table: "StarSystemThargoidLevel",
                column: "MaelstromId",
                principalTable: "ThargoidMaelstrom",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidMaelstrom_MaelstromId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropIndex(
                name: "IX_StarSystemThargoidLevel_MaelstromId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropColumn(
                name: "InfluenceSphere",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropColumn(
                name: "MaelstromId",
                table: "StarSystemThargoidLevel");
        }
    }
}
