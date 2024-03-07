using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddTitanDefeatCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefeatCycleId",
                table: "ThargoidMaelstrom",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstrom_DefeatCycleId",
                table: "ThargoidMaelstrom",
                column: "DefeatCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstrom_HeartsRemaining",
                table: "ThargoidMaelstrom",
                column: "HeartsRemaining");

            migrationBuilder.AddForeignKey(
                name: "FK_ThargoidMaelstrom_ThargoidCycle_DefeatCycleId",
                table: "ThargoidMaelstrom",
                column: "DefeatCycleId",
                principalTable: "ThargoidCycle",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThargoidMaelstrom_ThargoidCycle_DefeatCycleId",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropIndex(
                name: "IX_ThargoidMaelstrom_DefeatCycleId",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropIndex(
                name: "IX_ThargoidMaelstrom_HeartsRemaining",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropColumn(
                name: "DefeatCycleId",
                table: "ThargoidMaelstrom");
        }
    }
}
