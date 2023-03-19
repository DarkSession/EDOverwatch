using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddStarSystemMinorFactionPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorMinorFactionId",
                table: "Station",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StarSystemMinorFactionPresence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MinorFactionId = table.Column<int>(type: "int", nullable: true),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemMinorFactionPresence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemMinorFactionPresence_MinorFaction_MinorFactionId",
                        column: x => x.MinorFactionId,
                        principalTable: "MinorFaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StarSystemMinorFactionPresence_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Station_PriorMinorFactionId",
                table: "Station",
                column: "PriorMinorFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemMinorFactionPresence_MinorFactionId",
                table: "StarSystemMinorFactionPresence",
                column: "MinorFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemMinorFactionPresence_StarSystemId",
                table: "StarSystemMinorFactionPresence",
                column: "StarSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Station_MinorFaction_PriorMinorFactionId",
                table: "Station",
                column: "PriorMinorFactionId",
                principalTable: "MinorFaction",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Station_MinorFaction_PriorMinorFactionId",
                table: "Station");

            migrationBuilder.DropTable(
                name: "StarSystemMinorFactionPresence");

            migrationBuilder.DropIndex(
                name: "IX_Station_PriorMinorFactionId",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "PriorMinorFactionId",
                table: "Station");
        }
    }
}
