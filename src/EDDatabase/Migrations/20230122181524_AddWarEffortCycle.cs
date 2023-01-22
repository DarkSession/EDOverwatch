using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddWarEffortCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CycleId",
                table: "WarEffort",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarEffort_CycleId",
                table: "WarEffort",
                column: "CycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarEffort_ThargoidCycle_CycleId",
                table: "WarEffort",
                column: "CycleId",
                principalTable: "ThargoidCycle",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarEffort_ThargoidCycle_CycleId",
                table: "WarEffort");

            migrationBuilder.DropIndex(
                name: "IX_WarEffort_CycleId",
                table: "WarEffort");

            migrationBuilder.DropColumn(
                name: "CycleId",
                table: "WarEffort");
        }
    }
}
