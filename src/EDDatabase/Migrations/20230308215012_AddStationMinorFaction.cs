using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddStationMinorFaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRescueShip",
                table: "Station",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinorFactionId",
                table: "Station",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MinorFaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllegianceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinorFaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MinorFaction_FactionAllegiance_AllegianceId",
                        column: x => x.AllegianceId,
                        principalTable: "FactionAllegiance",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Station_IsRescueShip",
                table: "Station",
                column: "IsRescueShip");

            migrationBuilder.CreateIndex(
                name: "IX_Station_MinorFactionId",
                table: "Station",
                column: "MinorFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MinorFaction_AllegianceId",
                table: "MinorFaction",
                column: "AllegianceId");

            migrationBuilder.CreateIndex(
                name: "IX_MinorFaction_Name",
                table: "MinorFaction",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Station_MinorFaction_MinorFactionId",
                table: "Station",
                column: "MinorFactionId",
                principalTable: "MinorFaction",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Station_MinorFaction_MinorFactionId",
                table: "Station");

            migrationBuilder.DropTable(
                name: "MinorFaction");

            migrationBuilder.DropIndex(
                name: "IX_Station_IsRescueShip",
                table: "Station");

            migrationBuilder.DropIndex(
                name: "IX_Station_MinorFactionId",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "IsRescueShip",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "MinorFactionId",
                table: "Station");
        }
    }
}
