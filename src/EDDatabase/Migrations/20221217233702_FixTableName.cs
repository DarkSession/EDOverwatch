using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class FixTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DcohFaction_DochDiscordUser_CreatedById",
                table: "DcohFaction");

            migrationBuilder.DropForeignKey(
                name: "FK_DcohFactionOperation_DochDiscordUser_CreatedById",
                table: "DcohFactionOperation");

            migrationBuilder.RenameTable(
                name: "DochDiscordUser",
                newName: "DcohDiscordUser");

            migrationBuilder.CreateIndex(
                name: "IX_DcohDiscordUser_DiscordId",
                table: "DcohDiscordUser",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DcohDiscordUser_FactionId",
                table: "DcohDiscordUser",
                column: "FactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DcohFaction_DcohDiscordUser_CreatedById",
                table: "DcohFaction",
                column: "CreatedById",
                principalTable: "DcohDiscordUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DcohFactionOperation_DcohDiscordUser_CreatedById",
                table: "DcohFactionOperation",
                column: "CreatedById",
                principalTable: "DcohDiscordUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DcohFaction_DcohDiscordUser_CreatedById",
                table: "DcohFaction");

            migrationBuilder.DropForeignKey(
                name: "FK_DcohFactionOperation_DcohDiscordUser_CreatedById",
                table: "DcohFactionOperation");

            migrationBuilder.DropTable(
                name: "DcohDiscordUser");

            migrationBuilder.CreateTable(
                name: "DochDiscordUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactionId = table.Column<int>(type: "int", nullable: true),
                    DiscordId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    FactionJoined = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DochDiscordUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DochDiscordUser_DcohFaction_FactionId",
                        column: x => x.FactionId,
                        principalTable: "DcohFaction",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DochDiscordUser_DiscordId",
                table: "DochDiscordUser",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DochDiscordUser_FactionId",
                table: "DochDiscordUser",
                column: "FactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DcohFaction_DochDiscordUser_CreatedById",
                table: "DcohFaction",
                column: "CreatedById",
                principalTable: "DochDiscordUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DcohFactionOperation_DochDiscordUser_CreatedById",
                table: "DcohFactionOperation",
                column: "CreatedById",
                principalTable: "DochDiscordUser",
                principalColumn: "Id");
        }
    }
}
