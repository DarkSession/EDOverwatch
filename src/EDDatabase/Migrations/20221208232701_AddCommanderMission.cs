using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddCommanderMission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommanderMission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MissionId = table.Column<long>(type: "bigint", nullable: false),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    SystemId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderMission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderMission_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderMission_StarSystem_SystemId",
                        column: x => x.SystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMission_CommanderId",
                table: "CommanderMission",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMission_MissionId",
                table: "CommanderMission",
                column: "MissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMission_SystemId",
                table: "CommanderMission",
                column: "SystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderMission");
        }
    }
}
