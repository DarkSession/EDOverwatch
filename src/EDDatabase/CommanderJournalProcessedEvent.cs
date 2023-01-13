namespace EDDatabase
{
    [Table("CommanderJournalProcessedEvent")]
    [Index(nameof(Hash), IsUnique = true)]
    public class CommanderJournalProcessedEvent
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [Column]
        public DateTimeOffset Time { get; set; }

        [Column(TypeName = "varchar(64)")]
        public string Hash { get; set; }

        public CommanderJournalProcessedEvent(int id, DateTimeOffset time, string hash)
        {
            Id = id;
            Time = time;
            Hash = hash;
        }

        public static string GetEventHash(Commander commander, DateTimeOffset date, string eventName, WarEffortType warEffortType, int line)
        {
            return EDUtils.HashUtil.SHA256Hex($"{commander.Name.ToUpper()}:{date}:{eventName}:{warEffortType}:{line}");
        }
    }
}
