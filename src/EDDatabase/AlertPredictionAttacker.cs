namespace EDDatabase
{
    public class AlertPredictionAttacker
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [ForeignKey("StarSystemId")]
        public long? StarSystemId { get; set; }

        [Column]
        public int Order { get; set; }

        public AlertPredictionAttacker(int id, long? starSystemId, int order)
        {
            Id = id;
            StarSystemId = starSystemId;
            Order = order;
        }
    }
}
