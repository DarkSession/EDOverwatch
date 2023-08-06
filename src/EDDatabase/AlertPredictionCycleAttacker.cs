namespace EDDatabase
{
    [Table("AlertPredictionCycleAttacker")]
    public class AlertPredictionCycleAttacker
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CycleId")]
        public ThargoidCycle? Cycle { get; set; }

        [ForeignKey("AttackerStarSystemId")]
        public StarSystem? AttackerStarSystem { get; set; }

        [ForeignKey("VictimStarSystemId")]
        public StarSystem? VictimStarSystem { get; set; }

        public AlertPredictionCycleAttacker(int id)
        {
            Id = id;
        }
    }
}
