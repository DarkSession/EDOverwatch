using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: false),
                    DateHour = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerActivity_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActivity_Hash",
                table: "PlayerActivity",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActivity_StarSystemId_DateHour",
                table: "PlayerActivity",
                columns: new[] { "StarSystemId", "DateHour" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerActivity");
        }
    }
}
