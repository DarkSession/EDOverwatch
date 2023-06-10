using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddFCCargoStack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedUpdated",
                table: "CommanderFleetCarrierCargoItem",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "StackNumber",
                table: "CommanderFleetCarrierCargoItem",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUpdated",
                table: "CommanderFleetCarrierCargoItem");

            migrationBuilder.DropColumn(
                name: "StackNumber",
                table: "CommanderFleetCarrierCargoItem");
        }
    }
}
