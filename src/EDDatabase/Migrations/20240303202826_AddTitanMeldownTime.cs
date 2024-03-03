using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddTitanMeldownTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MeltdownTimeEstimate",
                table: "ThargoidMaelstrom",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ThargoidMaelstromHeart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Heart = table.Column<short>(type: "smallint", nullable: false),
                    MaelstromId = table.Column<int>(type: "int", nullable: true),
                    DestructionTime = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThargoidMaelstromHeart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThargoidMaelstromHeart_ThargoidMaelstrom_MaelstromId",
                        column: x => x.MaelstromId,
                        principalTable: "ThargoidMaelstrom",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstromHeart_MaelstromId",
                table: "ThargoidMaelstromHeart",
                column: "MaelstromId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThargoidMaelstromHeart");

            migrationBuilder.DropColumn(
                name: "MeltdownTimeEstimate",
                table: "ThargoidMaelstrom");
        }
    }
}
