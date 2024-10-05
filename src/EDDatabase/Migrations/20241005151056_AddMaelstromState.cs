using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddMaelstromState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "ThargoidMaelstrom",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstrom_State",
                table: "ThargoidMaelstrom",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThargoidMaelstrom_State",
                table: "ThargoidMaelstrom");

            migrationBuilder.DropColumn(
                name: "State",
                table: "ThargoidMaelstrom");
        }
    }
}
