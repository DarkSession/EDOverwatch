namespace EDDatabase
{
    [Table("StarSystem")]
    [Index(nameof(SystemAddress), IsUnique = true)]
    [Index(nameof(LocationX), nameof(LocationY), nameof(LocationZ))]
    public class StarSystem
    {
        [Column]
        public long Id { get; set; }

        [Column]
        public long SystemAddress { get; set; }

        [Column(TypeName = "varchar(512)")]
        public string Name { get; set; }

        [Column(TypeName = "decimal(14,6)")]
        public decimal LocationX { get; set; }

        [Column(TypeName = "decimal(14,6)")]
        public decimal LocationY { get; set; }

        [Column(TypeName = "decimal(14,6)")]
        public decimal LocationZ { get; set; }

        [Column]
        public long Population { get; set; }

        [ForeignKey("AllegianceId")]
        public FactionAllegiance? Allegiance { get; set; }

        [ForeignKey("SecurityId")]
        public StarSystemSecurity? Security { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [ForeignKey("ThargoidLevelId")]
        public StarSystemThargoidLevel? ThargoidLevel { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        public IEnumerable<StarSystemThargoidLevel>? ThargoidLevelHistory { get; set; }

        public IEnumerable<StarSystemFssSignal>? FssSignals { get; set; }

        public StarSystem(
            long id,
            long systemAddress,
            string name,
            decimal locationX,
            decimal locationY,
            decimal locationZ,
            long population,
            DateTimeOffset created,
            DateTimeOffset updated)
        {
            Id = id;
            SystemAddress = systemAddress;
            Name = name;
            LocationX = locationX;
            LocationY = locationY;
            LocationZ = locationZ;
            Population = population;
            Created = created;
            Updated = updated;
        }
    }
}
