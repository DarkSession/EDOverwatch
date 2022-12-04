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

        [ForeignKey("CycleEndId")]
        public ThargoidCycle? CycleEnd { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        public StarSystemThargoidLevel(int id, StarSystemThargoidLevelState state)
        {
            Id = id;
            State = state;
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
    }

    /*
     Invasion time:
        33.000 pop: 5 weeks
        95.000 pop: 5 weeks
       128.000 pop: 7 weeks
       145.000 pop: 6 weeks
     2.900.000 pop: 7 weeks
     5.100.000 pop: 5 weeks
     ....
    */
}
