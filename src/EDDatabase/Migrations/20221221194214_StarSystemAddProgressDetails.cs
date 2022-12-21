using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class StarSystemAddProgressDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentProgressId",
                table: "StarSystemThargoidLevel",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateExpiresId",
                table: "StarSystemThargoidLevel",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StarSystemThargoidLevelProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Updated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ThargoidLevelId = table.Column<int>(type: "int", nullable: true),
                    Progress = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemThargoidLevelProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemThargoidLevelProgress_StarSystemThargoidLevel_Thar~",
                        column: x => x.ThargoidLevelId,
                        principalTable: "StarSystemThargoidLevel",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_CurrentProgressId",
                table: "StarSystemThargoidLevel",
                column: "CurrentProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_StateExpiresId",
                table: "StarSystemThargoidLevel",
                column: "StateExpiresId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevelProgress_ThargoidLevelId",
                table: "StarSystemThargoidLevelProgress",
                column: "ThargoidLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystemThargoidLevel_StarSystemThargoidLevelProgress_Curr~",
                table: "StarSystemThargoidLevel",
                column: "CurrentProgressId",
                principalTable: "StarSystemThargoidLevelProgress",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidCycle_StateExpiresId",
                table: "StarSystemThargoidLevel",
                column: "StateExpiresId",
                principalTable: "ThargoidCycle",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StarSystemThargoidLevel_StarSystemThargoidLevelProgress_Curr~",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropForeignKey(
                name: "FK_StarSystemThargoidLevel_ThargoidCycle_StateExpiresId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropTable(
                name: "StarSystemThargoidLevelProgress");

            migrationBuilder.DropIndex(
                name: "IX_StarSystemThargoidLevel_CurrentProgressId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropIndex(
                name: "IX_StarSystemThargoidLevel_StateExpiresId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropColumn(
                name: "CurrentProgressId",
                table: "StarSystemThargoidLevel");

            migrationBuilder.DropColumn(
                name: "StateExpiresId",
                table: "StarSystemThargoidLevel");
        }
    }
}
