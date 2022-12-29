using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemUpdateQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManualUpdateCycleId",
                table: "StarSystemThargoidLevel",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StarSystemUpdateQueueItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DiscordChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Result = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ResultBy = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Queued = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Completed = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemUpdateQueueItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemUpdateQueueItem_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_ManualUpdateCycleId",
                table: "StarSystemThargoidLevel",
                column: "ManualUpdateCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemUpdateQueueItem_StarSystemId",
                table: "StarSystemUpdateQueueItem",
                column: "StarSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidCycle_ManualUpdateCycleId",
                table: "StarSystemThargoidLevel",
                column: "ManualUpdateCycleId",
                principalTable: "ThargoidCycle",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidCycle_ManualUpdateCycleId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropTable(
                name: "StarSystemUpdateQueueItem");

            migrationBuilder.DropIndex(
                name: "IX_StarSystemThargoidLevel_ManualUpdateCycleId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropColumn(
                name: "ManualUpdateCycleId",
                table: "StarSystemThargoidLevel");
        }
    }
}
