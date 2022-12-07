namespace EDDatabase
{
    [Table("WarEffort")]
    [Index(nameof(Side))]
    public class WarEffort
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public WarEffortType Type { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [ForeignKey("StarSystemId")]
        public long? StarSystemId { get; set; }

        [Column]
        public DateOnly Date { get; set; }

        [Column]
        public long Amount { get; set; }

        [Column]
        public WarEffortSide Side { get; set; }

        [Column]
        public WarEffortSource Source { get; set; }

        public WarEffort(int id, WarEffortType type, DateOnly date, long amount, WarEffortSide side, WarEffortSource source)
        {
            Id = id;
            Type = type;
            Date = date;
            Amount = amount;
            Side = side;
            Source = source;
        }
    }

    public enum WarEffortType : byte
    {
        Kill = 1,
        Rescue,
        SupplyDelivery,
    }

    public enum WarEffortSide : byte
    {
        Humans = 1,
        Thargoids,
    }

    public enum WarEffortSource : byte
    {
        Unknown = 0,
        IDA,
        Inara,
    }
}
