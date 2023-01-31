using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationMeetingPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingPoint",
                table: "DcohFactionOperation",
                type: "varchar(512)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StarSystem_Name",
                table: "StarSystem",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StarSystem_Name",
                table: "StarSystem");

            migrationBuilder.DropColumn(
                name: "MeetingPoint",
                table: "DcohFactionOperation");
        }
    }
}
