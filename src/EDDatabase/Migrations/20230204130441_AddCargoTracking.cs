using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commodity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commodity", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommanderCargo",
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
                    table.PrimaryKey("PK_CommanderCargo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderCargo_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderCargo_Commodity_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderCargo_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderCargo_CommanderId",
                table: "CommanderCargo",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderCargo_CommodityId",
                table: "CommanderCargo",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderCargo_StarSystemId",
                table: "CommanderCargo",
                column: "StarSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderCargo");

            migrationBuilder.DropTable(
                name: "Commodity");
        }
    }
}
