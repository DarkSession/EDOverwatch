using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddFCCargoTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "HasFleetCarrier",
                table: "Commander",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "CommanderFleetCarrierCargoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    CommodityId = table.Column<int>(type: "int", nullable: true),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderFleetCarrierCargoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderFleetCarrierCargoItem_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderFleetCarrierCargoItem_Commodity_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderFleetCarrierCargoItem_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderFleetCarrierCargoItem_CommanderId",
                table: "CommanderFleetCarrierCargoItem",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderFleetCarrierCargoItem_CommodityId",
                table: "CommanderFleetCarrierCargoItem",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderFleetCarrierCargoItem_StarSystemId",
                table: "CommanderFleetCarrierCargoItem",
                column: "StarSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderFleetCarrierCargoItem");

            migrationBuilder.DropColumn(
                name: "HasFleetCarrier",
                table: "Commander");
        }
    }
}
