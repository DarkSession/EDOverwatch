using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class CommanderCApiChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogLastLine",
                table: "Commander",
                newName: "JournalLastLine");

            migrationBuilder.RenameColumn(
                name: "LogLastDateProcessed",
                table: "Commander",
                newName: "JournalLastProcessed");

            migrationBuilder.AddColumn<int>(
                name: "CommanderId",
                table: "WarEffort",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JournalDay",
                table: "Commander",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JournalLastActivity",
                table: "Commander",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Commander",
                type: "varchar(256)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte>(
                name: "OAuthStatus",
                table: "Commander",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_WarEffort_CommanderId",
                table: "WarEffort",
                column: "CommanderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarEffort_Commander_CommanderId",
                table: "WarEffort",
                column: "CommanderId",
                principalTable: "Commander",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarEffort_Commander_CommanderId",
                table: "WarEffort");

            migrationBuilder.DropIndex(
                name: "IX_WarEffort_CommanderId",
                table: "WarEffort");

            migrationBuilder.DropColumn(
                name: "CommanderId",
                table: "WarEffort");

            migrationBuilder.DropColumn(
                name: "JournalDay",
                table: "Commander");

            migrationBuilder.DropColumn(
                name: "JournalLastActivity",
                table: "Commander");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Commander");

            migrationBuilder.DropColumn(
                name: "OAuthStatus",
                table: "Commander");

            migrationBuilder.RenameColumn(
                name: "JournalLastProcessed",
                table: "Commander",
                newName: "LogLastDateProcessed");

            migrationBuilder.RenameColumn(
                name: "JournalLastLine",
                table: "Commander",
                newName: "LogLastLine");
        }
    }
}
