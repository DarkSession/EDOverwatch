using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "Progress",
                table: "StarSystemThargoidLevel",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OAuthCode",
                columns: table => new
                {
                    State = table.Column<string>(type: "varchar(128)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(128)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthCode", x => x.State);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommanderId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedUserName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedEmail = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Commander",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FDevCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SystemId = table.Column<long>(type: "bigint", nullable: true),
                    StationId = table.Column<long>(type: "bigint", nullable: true),
                    IsInLiveVersion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LogLastDateProcessed = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LogLastLine = table.Column<int>(type: "int", nullable: false),
                    OAuthCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OAuthAccessToken = table.Column<string>(type: "varchar(4096)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OAuthRefreshToken = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OAuthTokenType = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commander", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commander_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Commander_StarSystem_SystemId",
                        column: x => x.SystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Commander_Station_StationId",
                        column: x => x.StationId,
                        principalTable: "Station",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_CommanderId",
                table: "ApplicationUser",
                column: "CommanderId");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_FDevCustomerId",
                table: "Commander",
                column: "FDevCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commander_StationId",
                table: "Commander",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_SystemId",
                table: "Commander",
                column: "SystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Commander_UserId",
                table: "Commander",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUser_Commander_CommanderId",
                table: "ApplicationUser",
                column: "CommanderId",
                principalTable: "Commander",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUser_Commander_CommanderId",
                table: "ApplicationUser");

            migrationBuilder.DropTable(
                name: "OAuthCode");

            migrationBuilder.DropTable(
                name: "Commander");

            migrationBuilder.DropTable(
                name: "ApplicationUser");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "StarSystemThargoidLevel");
        }
    }
}
