using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertPrediction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CycleId = table.Column<int>(type: "int", nullable: true),
                    MaelstromId = table.Column<int>(type: "int", nullable: true),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    AlertLikely = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPrediction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertPrediction_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertPrediction_ThargoidCycle_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ThargoidCycle",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertPrediction_ThargoidMaelstrom_MaelstromId",
                        column: x => x.MaelstromId,
                        principalTable: "ThargoidMaelstrom",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertPredictionCycleAttacker",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CycleId = table.Column<int>(type: "int", nullable: true),
                    AttackerStarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    VictimStarSystemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPredictionCycleAttacker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertPredictionCycleAttacker_StarSystem_AttackerStarSystemId",
                        column: x => x.AttackerStarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertPredictionCycleAttacker_StarSystem_VictimStarSystemId",
                        column: x => x.VictimStarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertPredictionCycleAttacker_ThargoidCycle_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ThargoidCycle",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlertPredictionAttacker",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    AlertPredictionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPredictionAttacker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertPredictionAttacker_AlertPrediction_AlertPredictionId",
                        column: x => x.AlertPredictionId,
                        principalTable: "AlertPrediction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertPredictionAttacker_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPrediction_CycleId",
                table: "AlertPrediction",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPrediction_MaelstromId",
                table: "AlertPrediction",
                column: "MaelstromId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPrediction_StarSystemId",
                table: "AlertPrediction",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPredictionAttacker_AlertPredictionId",
                table: "AlertPredictionAttacker",
                column: "AlertPredictionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPredictionAttacker_StarSystemId",
                table: "AlertPredictionAttacker",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPredictionCycleAttacker_AttackerStarSystemId",
                table: "AlertPredictionCycleAttacker",
                column: "AttackerStarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPredictionCycleAttacker_CycleId",
                table: "AlertPredictionCycleAttacker",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertPredictionCycleAttacker_VictimStarSystemId",
                table: "AlertPredictionCycleAttacker",
                column: "VictimStarSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertPredictionAttacker");

            migrationBuilder.DropTable(
                name: "AlertPredictionCycleAttacker");

            migrationBuilder.DropTable(
                name: "AlertPrediction");
        }
    }
}
