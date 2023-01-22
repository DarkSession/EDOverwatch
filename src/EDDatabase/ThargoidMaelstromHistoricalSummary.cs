namespace EDDatabase
{
    [Table("ThargoidMaelstromHistoricalSummary")]
    public class ThargoidMaelstromHistoricalSummary
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [ForeignKey("CycleId")]
        public ThargoidCycle? Cycle { get; set; }

        [Column]
        public StarSystemThargoidLevelState State { get; set; }

        [Column]
        public int Amount { get; set; }

        public ThargoidMaelstromHistoricalSummary(int id, StarSystemThargoidLevelState state, int amount)
        {
            Id = id;
            State = state;
            Amount = amount;
        }
    }
}
