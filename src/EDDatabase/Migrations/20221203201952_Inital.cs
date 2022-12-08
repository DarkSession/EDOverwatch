using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Inital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Economy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Economy", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FactionAllegiance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionAllegiance", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FactionGovernment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionGovernment", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StarSystemSecurity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemSecurity", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StationType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameEnglish = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationType", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThargoidCycle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Start = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    End = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThargoidCycle", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StarSystem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemAddress = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationX = table.Column<decimal>(type: "decimal(14,6)", nullable: false),
                    LocationY = table.Column<decimal>(type: "decimal(14,6)", nullable: false),
                    LocationZ = table.Column<decimal>(type: "decimal(14,6)", nullable: false),
                    Population = table.Column<long>(type: "bigint", nullable: false),
                    AllegianceId = table.Column<int>(type: "int", nullable: true),
                    SecurityId = table.Column<int>(type: "int", nullable: true),
                    MaelstromId = table.Column<int>(type: "int", nullable: true),
                    ThargoidLevelId = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystem_FactionAllegiance_AllegianceId",
                        column: x => x.AllegianceId,
                        principalTable: "FactionAllegiance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StarSystem_StarSystemSecurity_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "StarSystemSecurity",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StarSystemFssSignal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    FirstSeen = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemFssSignal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemFssSignal_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StarSystemThargoidLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CycleStartId = table.Column<int>(type: "int", nullable: true),
                    CycleEndId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarSystemThargoidLevel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StarSystemThargoidLevel_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StarSystemThargoidLevel_ThargoidCycle_CycleEndId",
                        column: x => x.CycleEndId,
                        principalTable: "ThargoidCycle",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StarSystemThargoidLevel_ThargoidCycle_CycleStartId",
                        column: x => x.CycleStartId,
                        principalTable: "ThargoidCycle",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Station",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "varchar(512)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MarketId = table.Column<long>(type: "bigint", nullable: false),
                    DistanceFromStarLS = table.Column<decimal>(type: "decimal(14,6)", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    GovernmentId = table.Column<int>(type: "int", nullable: true),
                    PrimaryEconomyId = table.Column<int>(type: "int", nullable: true),
                    SecondaryEconomyId = table.Column<int>(type: "int", nullable: true),
                    LandingPadSmall = table.Column<short>(type: "smallint", nullable: false),
                    LandingPadMedium = table.Column<short>(type: "smallint", nullable: false),
                    LandingPadLarge = table.Column<short>(type: "smallint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Station", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Station_Economy_PrimaryEconomyId",
                        column: x => x.PrimaryEconomyId,
                        principalTable: "Economy",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Station_Economy_SecondaryEconomyId",
                        column: x => x.SecondaryEconomyId,
                        principalTable: "Economy",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Station_FactionGovernment_GovernmentId",
                        column: x => x.GovernmentId,
                        principalTable: "FactionGovernment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Station_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Station_StationType_TypeId",
                        column: x => x.TypeId,
                        principalTable: "StationType",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThargoidMaelstrom",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Updated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    StarSystemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThargoidMaelstrom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThargoidMaelstrom_StarSystem_StarSystemId",
                        column: x => x.StarSystemId,
                        principalTable: "StarSystem",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Economy_Name",
                table: "Economy",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionAllegiance_Name",
                table: "FactionAllegiance",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionGovernment_Name",
                table: "FactionGovernment",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_AllegianceId",
                table: "StarSystem",
                column: "AllegianceId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_LocationX_LocationY_LocationZ",
                table: "StarSystem",
                columns: new[] { "LocationX", "LocationY", "LocationZ" });

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_MaelstromId",
                table: "StarSystem",
                column: "MaelstromId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_SecurityId",
                table: "StarSystem",
                column: "SecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_SystemAddress",
                table: "StarSystem",
                column: "SystemAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_ThargoidLevelId",
                table: "StarSystem",
                column: "ThargoidLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemFssSignal_StarSystemId",
                table: "StarSystemFssSignal",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemSecurity_Name",
                table: "StarSystemSecurity",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_CycleEndId",
                table: "StarSystemThargoidLevel",
                column: "CycleEndId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_CycleStartId",
                table: "StarSystemThargoidLevel",
                column: "CycleStartId");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystemThargoidLevel_StarSystemId",
                table: "StarSystemThargoidLevel",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Station_GovernmentId",
                table: "Station",
                column: "GovernmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Station_PrimaryEconomyId",
                table: "Station",
                column: "PrimaryEconomyId");

            migrationBuilder.CreateIndex(
                name: "IX_Station_SecondaryEconomyId",
                table: "Station",
                column: "SecondaryEconomyId");

            migrationBuilder.CreateIndex(
                name: "IX_Station_StarSystemId",
                table: "Station",
                column: "StarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Station_TypeId",
                table: "Station",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StationType_Name",
                table: "StationType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThargoidMaelstrom_StarSystemId",
                table: "ThargoidMaelstrom",
                column: "StarSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystem_StarSystemThargoidLevel_ThargoidLevelId",
                table: "StarSystem",
                column: "ThargoidLevelId",
                principalTable: "StarSystemThargoidLevel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StarSystem_ThargoidMaelstrom_MaelstromId",
                table: "StarSystem",
                column: "MaelstromId",
                principalTable: "ThargoidMaelstrom",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StarSystem_FactionAllegiance_AllegianceId",
                table: "StarSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_StarSystem_StarSystemSecurity_SecurityId",
                table: "StarSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_StarSystem_StarSystemThargoidLevel_ThargoidLevelId",
                table: "StarSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_StarSystem_ThargoidMaelstrom_MaelstromId",
                table: "StarSystem");

            migrationBuilder.DropTable(
                name: "StarSystemFssSignal");

            migrationBuilder.DropTable(
                name: "Station");

            migrationBuilder.DropTable(
                name: "Economy");

            migrationBuilder.DropTable(
                name: "FactionGovernment");

            migrationBuilder.DropTable(
                name: "StationType");

            migrationBuilder.DropTable(
                name: "FactionAllegiance");

            migrationBuilder.DropTable(
                name: "StarSystemSecurity");

            migrationBuilder.DropTable(
                name: "StarSystemThargoidLevel");

            migrationBuilder.DropTable(
                name: "ThargoidCycle");

            migrationBuilder.DropTable(
                name: "ThargoidMaelstrom");

            migrationBuilder.DropTable(
                name: "StarSystem");
        }
    }
}
