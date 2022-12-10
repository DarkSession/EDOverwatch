using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddCommanderJournalDeferal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommanderDeferredJournalEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    SystemId = table.Column<long>(type: "bigint", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Event = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Journal = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderDeferredJournalEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderDeferredJournalEvent_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderDeferredJournalEvent_StarSystem_SystemId",
                        column: x => x.SystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderDeferredJournalEvent_CommanderId",
                table: "CommanderDeferredJournalEvent",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderDeferredJournalEvent_Status",
                table: "CommanderDeferredJournalEvent",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderDeferredJournalEvent_SystemId",
                table: "CommanderDeferredJournalEvent",
                column: "SystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderDeferredJournalEvent");
        }
    }
}
