using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddDcohFaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OriginalPopulation",
                table: "StarSystem",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE StarSystem SET OriginalPopulation = Population;");

            migrationBuilder.CreateTable(
                name: "DcohFaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Short = table.Column<string>(type: "varchar(8)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcohFaction", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DcohFactionOperation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcohFactionOperation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DcohFactionOperation_DcohFaction_FactionId",
                        column: x => x.FactionId,
                        principalTable: "DcohFaction",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DcohFactionOperation_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Station_MarketId",
                table: "Station",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_DcohFactionOperation_FactionId",
                table: "DcohFactionOperation",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_DcohFactionOperation_StarSystemId",
                table: "DcohFactionOperation",
                column: "StarSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DcohFactionOperation");

            migrationBuilder.DropTable(
                name: "DcohFaction");

            migrationBuilder.DropIndex(
                name: "IX_Station_MarketId",
                table: "Station");

            migrationBuilder.DropColumn(
                name: "OriginalPopulation",
                table: "StarSystem");
        }
    }
}
