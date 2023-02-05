using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddStationBodies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BodyId",
                table: "Station",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StarSystemBody",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyId = table.Column<int>(type: "int", nullable: false),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Gravity = table.Column<decimal>(type: "decimal(14,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemBody", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemBody_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Station_BodyId",
                table: "Station",
                column: "BodyId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemBody_StarSystemId",
                table: "StarSystemBody",
                column: "StarSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Station_StarSystemBody_BodyId",
                table: "Station",
                column: "BodyId",
                principalTable: "StarSystemBody",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Station_StarSystemBody_BodyId",
                table: "Station");

            migrationBuilder.DropTable(
                name: "StarSystemBody");

            migrationBuilder.DropIndex(
                name: "IX_Station_BodyId",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "BodyId",
                table: "Station");
        }
    }
}
