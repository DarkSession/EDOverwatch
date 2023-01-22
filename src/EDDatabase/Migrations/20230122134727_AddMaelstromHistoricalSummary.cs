using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddMaelstromHistoricalSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThargoidMaelstromHistoricalSummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaelstromId = table.Column<int>(type: "int", nullable: true),
                    CycleId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThargoidMaelstromHistoricalSummary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThargoidMaelstromHistoricalSummary_ThargoidCycle_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ThargoidCycle",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThargoidMaelstromHistoricalSummary_ThargoidMaelstrom_Maelstr~",
                        column: x => x.MaelstromId,
                        principalTable: "ThargoidMaelstrom",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstromHistoricalSummary_CycleId",
                table: "ThargoidMaelstromHistoricalSummary",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstromHistoricalSummary_MaelstromId",
                table: "ThargoidMaelstromHistoricalSummary",
                column: "MaelstromId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThargoidMaelstromHistoricalSummary");
        }
    }
}
