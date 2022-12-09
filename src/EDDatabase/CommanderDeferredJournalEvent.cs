namespace EDDatabase
{
    [Table("CommanderDeferredJournalEvent")]
    [Index(nameof(Status))]
    public class CommanderDeferredJournalEvent
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [ForeignKey("SystemId")]
        public StarSystem? System { get; set; }

        [Column]
        public DateTimeOffset Timestamp { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Event { get; set; }

        [Column]
        public WarEffortSource Source { get; set; }

        [Column(TypeName = "text")]
        public string Journal { get; set; }

        [Column]
        public CommanderDeferredJournalEventStatus Status { get; set; }

        public CommanderDeferredJournalEvent(int id, DateTimeOffset timestamp, string @event, WarEffortSource source, string journal, CommanderDeferredJournalEventStatus status)
        {
            Id = id;
            Timestamp = timestamp;
            Event = @event;
            Source = source;
            Journal = journal;
            Status = status;
        }
    }

    public enum CommanderDeferredJournalEventStatus : byte
    {
        Pending,
        Processed,
    }
}
