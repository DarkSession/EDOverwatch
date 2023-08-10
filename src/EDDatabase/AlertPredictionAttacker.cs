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

        [Column]
        public AlertPredictionAttackerStatus Status { get; set; }

        public AlertPredictionAttacker(int id, long? starSystemId, int order, AlertPredictionAttackerStatus status)
        {
            Id = id;
            StarSystemId = starSystemId;
            Order = order;
            Status = status;
        }
    }

    public enum AlertPredictionAttackerStatus : byte
    {
        Default = 0,
    }
}
