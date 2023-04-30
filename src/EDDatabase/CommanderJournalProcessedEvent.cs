using System.Globalization;

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
            string eventHashBase;
            if (date >= new DateTimeOffset(2023, 5, 1, 0, 0, 0, TimeSpan.Zero))
            {
                eventHashBase = $"{commander.Name.ToUpper()}:{starSystem?.Id ?? 0}:{date.ToString("s")}:{eventName}:{warEffortType}";
            }
            else
            {
                eventHashBase = $"{commander.Name.ToUpper()}:{starSystem?.Id ?? 0}:{date.ToString("MM/dd/yyyy HH:mm:ss")} +00:00:{eventName}:{warEffortType}";
            }
            return EDUtils.HashUtil.SHA256Hex(eventHashBase);
        }
    }
}
