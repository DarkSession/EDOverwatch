using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AlertPredictionAndProgressUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProgressPercent",
                table: "StarSystemThargoidLevelProgress",
                type: "decimal(10,6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "AlertPrediction",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "StarSystemThargoidLevelProgress");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "AlertPrediction");
        }
    }
}
