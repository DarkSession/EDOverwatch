using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class DiscordBotPart1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DcohFactionOperation");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DcohFaction");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "DcohFactionOperation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "DcohFaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DochDiscordUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscordId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    FactionId = table.Column<int>(type: "int", nullable: true),
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
                name: "IX_DcohFactionOperation_CreatedById",
                table: "DcohFactionOperation",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DcohFaction_CreatedById",
                table: "DcohFaction",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DcohFaction_Short",
                table: "DcohFaction",
                column: "Short",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DcohFaction_DochDiscordUser_CreatedById",
                table: "DcohFaction");

            migrationBuilder.DropForeignKey(
                name: "FK_DcohFactionOperation_DochDiscordUser_CreatedById",
                table: "DcohFactionOperation");

            migrationBuilder.DropTable(
                name: "DochDiscordUser");

            migrationBuilder.DropIndex(
                name: "IX_DcohFactionOperation_CreatedById",
                table: "DcohFactionOperation");

            migrationBuilder.DropIndex(
                name: "IX_DcohFaction_CreatedById",
                table: "DcohFaction");

            migrationBuilder.DropIndex(
                name: "IX_DcohFaction_Short",
                table: "DcohFaction");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "DcohFactionOperation");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "DcohFaction");

            migrationBuilder.AddColumn<ulong>(
                name: "CreatedBy",
                table: "DcohFactionOperation",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "CreatedBy",
                table: "DcohFaction",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);
        }
    }
}
