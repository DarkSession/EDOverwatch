using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commander_FDevCustomerId",
                table: "Commander");

            migrationBuilder.AddColumn<int>(
                name: "ApiKeyId",
                table: "Commander",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommanderApiKey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderApiKey", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommanderJournalProcessedEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    Time = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Hash = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderJournalProcessedEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderJournalProcessedEvent_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommanderApiKeyClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    ApiKeyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderApiKeyClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommanderApiKeyClaim_CommanderApiKey_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "CommanderApiKey",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommanderApiKeyClaim_Commander_CommanderId",
                        column: x => x.CommanderId,
                        principalTable: "Commander",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_ApiKeyId",
                table: "Commander",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_FDevCustomerId",
                table: "Commander",
                column: "FDevCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderApiKey_Key",
                table: "CommanderApiKey",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommanderApiKeyClaim_ApiKeyId",
                table: "CommanderApiKeyClaim",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderApiKeyClaim_CommanderId",
                table: "CommanderApiKeyClaim",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderJournalProcessedEvent_CommanderId",
                table: "CommanderJournalProcessedEvent",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderJournalProcessedEvent_Hash",
                table: "CommanderJournalProcessedEvent",
                column: "Hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Commander_CommanderApiKey_ApiKeyId",
                table: "Commander",
                column: "ApiKeyId",
                principalTable: "CommanderApiKey",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commander_CommanderApiKey_ApiKeyId",
                table: "Commander");

            migrationBuilder.DropTable(
                name: "CommanderApiKeyClaim");

            migrationBuilder.DropTable(
                name: "CommanderJournalProcessedEvent");

            migrationBuilder.DropTable(
                name: "CommanderApiKey");

            migrationBuilder.DropIndex(
                name: "IX_Commander_ApiKeyId",
                table: "Commander");

            migrationBuilder.DropIndex(
                name: "IX_Commander_FDevCustomerId",
                table: "Commander");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "Commander");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_FDevCustomerId",
                table: "Commander",
                column: "FDevCustomerId",
                unique: true);
        }
    }
}
