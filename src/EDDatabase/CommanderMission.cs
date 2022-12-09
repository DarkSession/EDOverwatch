namespace EDDatabase
{
    [Table("CommanderMission")]
    [Index(nameof(MissionId), IsUnique = true)]
    public class CommanderMission
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public long MissionId { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [Column]
        public DateTimeOffset Date { get; set; }

        [Column("SystemId")]
        public StarSystem? System { get; set; }

        [Column]
        public CommanderMissionStatus Status { get; set; }

        public CommanderMission(int id, long missionId, DateTimeOffset date, CommanderMissionStatus status)
        {
            Id = id;
            MissionId = missionId;
            Date = date;
            Status = status;
        }
    }

    public enum CommanderMissionStatus
    {
        Accepted,
        Completed,
    }
}
