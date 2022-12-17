namespace EDDatabase
{
    [Table("StarSystemThargoidLevel")]
    [Index(nameof(State))]
    public class StarSystemThargoidLevel
    {
        [Column]
        public int Id { get; set; }

        public StarSystem? StarSystem { get; set; }

        [Column]
        public StarSystemThargoidLevelState State { get; set; }

        [ForeignKey("CycleStartId")]
        public ThargoidCycle? CycleStart { get; set; }

        [ForeignKey("CycleStartId { get; set; }\r\n")]
        public int? CycleStartId { get; set; }

        [ForeignKey("CycleEndId")]
        public ThargoidCycle? CycleEnd { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [Column]
        public short? Progress { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        public StarSystemThargoidLevel(int id, StarSystemThargoidLevelState state, short? progress, DateTimeOffset created)
        {
            Id = id;
            State = state;
            Progress = progress;
            Created = created;
        }
    }

    public enum StarSystemThargoidLevelState : byte
    {
        None = 0,
        Alert = 20,
        Invasion = 30,
        Controlled = 40,
        Maelstrom = 50,
        Recapture = 60,
        Recovery = 70,
    }
}
