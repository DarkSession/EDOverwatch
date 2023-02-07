namespace EDDatabase
{
    [Table("CommanderJournalProcessedEvent")]
    [Index(nameof(Hash), nameof(Line), IsUnique = true)]
    public class CommanderJournalProcessedEvent
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [Column]
        public DateTimeOffset Time { get; set; }

        [Column]
        public int Line { get; set; }

        [Column(TypeName = "varchar(64)")]
        public string Hash { get; set; }

        public CommanderJournalProcessedEvent(int id, DateTimeOffset time, int line, string hash)
        {
            Id = id;
            Time = time;
            Line = line;
            Hash = hash;
        }

        public static string GetEventHash(Commander commander, StarSystem? starSystem, DateTimeOffset date, string eventName, WarEffortType warEffortType)
        {
            return EDUtils.HashUtil.SHA256Hex($"{commander.Name.ToUpper()}:{starSystem?.Id ?? 0}:{date}:{eventName}:{warEffortType}");
        }
    }
}
