namespace EDDatabase
{
    [Table("Station")]
    [Index(nameof(MarketId))]
    [Index(nameof(IsRescueShip))]
    [Index(nameof(Name))]
    public class Station
    {
        [Column]
        public long Id { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column(TypeName = "varchar(512)")]
        public string Name { get; set; }

        [Column]
        public long MarketId { get; set; }

        [Column(TypeName = "decimal(14,6)")]
        public decimal DistanceFromStarLS { get; set; }

        [ForeignKey("TypeId")]
        public StationType? Type { get; set; }

        [ForeignKey("GovernmentId")]
        public FactionGovernment? Government { get; set; }

        [ForeignKey("PrimaryEconomyId")]
        public Economy? PrimaryEconomy { get; set; }

        [ForeignKey("SecondaryEconomyId")]
        public Economy? SecondaryEconomy { get; set; }

        [ForeignKey("BodyId")]
        public StarSystemBody? Body { get; set; }

        [ForeignKey("MinorFactionId")]
        public MinorFaction? MinorFaction { get; set; }

        [ForeignKey("PriorMinorFactionId")]
        public MinorFaction? PriorMinorFaction { get; set; }

        [Column]
        public short LandingPadSmall { get; set; }

        [Column]
        public short LandingPadMedium { get; set; }

        [Column]
        public short LandingPadLarge { get; set; }

        [Column]
        public StationState State { get; set; }

        [Column]
        public RescueShipType IsRescueShip { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        public Station(
            long id,
            string name,
            long marketId,
            decimal distanceFromStarLS,
            short landingPadSmall,
            short landingPadMedium,
            short landingPadLarge,
            StationState state,
            RescueShipType isRescueShip,
            DateTimeOffset created,
            DateTimeOffset updated)
        {
            Id = id;
            Name = name;
            MarketId = marketId;
            DistanceFromStarLS = distanceFromStarLS;
            LandingPadSmall = landingPadSmall;
            LandingPadMedium = landingPadMedium;
            LandingPadLarge = landingPadLarge;
            State = state;
            IsRescueShip = isRescueShip;
            Created = created;
            Updated = updated;
        }
    }

    public enum StationState : byte
    {
        Normal,
        UnderAttack,
        Damaged,
        UnderRepairs,
        Abandoned,
    }

    public enum RescueShipType : byte
    {
        No,
        Primary,
        Secondary,
    }
}
