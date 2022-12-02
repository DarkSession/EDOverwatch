namespace EDDatabase
{
    [Table("StarSystemThargoidLevel")]
    public class StarSystemThargoidLevel
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column]
        public StarSystemThargoidLevelState State { get; set; }

        [ForeignKey("CycleStartId")]
        public ThargoidCycle? CycleStart { get; set; }

        [ForeignKey("CycleEndId")]
        public ThargoidCycle? CycleEnd { get; set; }

        public StarSystemThargoidLevel(int id, StarSystemThargoidLevelState state)
        {
            Id = id;
            State = state;
        }
    }

    public enum StarSystemThargoidLevelState : byte
    {
        Unknown = 0,
        None = 10,
        Alert = 20,
        Invasion = 30,
        Controlled = 40,
        Maelstrom = 50,
        Recapture = 60,
    }
}
