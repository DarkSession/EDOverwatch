using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedJournalEventLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommanderJournalProcessedEvent_Hash",
                table: "CommanderJournalProcessedEvent");

            migrationBuilder.AddColumn<int>(
                name: "Line",
                table: "CommanderJournalProcessedEvent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CommanderJournalProcessedEvent_Hash_Line",
                table: "CommanderJournalProcessedEvent",
                columns: new[] { "Hash", "Line" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommanderJournalProcessedEvent_Hash_Line",
                table: "CommanderJournalProcessedEvent");

            migrationBuilder.DropColumn(
                name: "Line",
                table: "CommanderJournalProcessedEvent");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderJournalProcessedEvent_Hash",
                table: "CommanderJournalProcessedEvent",
                column: "Hash",
                unique: true);
        }
    }
}
