using System.Numerics;

namespace EDDatabase
{
    [Table("StarSystem")]
    [Index(nameof(SystemAddress), IsUnique = true)]
    [Index(nameof(Name))]
    [Index(nameof(LocationX), nameof(LocationY), nameof(LocationZ))]
    [Index(nameof(WarRelevantSystem))]
    [Index(nameof(WarAffected))]
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

        [Column]
        public long OriginalPopulation { get; set; }

        [Column]
        public long PopulationMin { get; set; }

        [ForeignKey("AllegianceId")]
        public FactionAllegiance? Allegiance { get; set; }

        [ForeignKey("SecurityId")]
        public StarSystemSecurity? Security { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? DoNotUse { get; set; }

        [ForeignKey("ThargoidLevelId")]
        public StarSystemThargoidLevel? ThargoidLevel { get; set; }

        [Column]
        public bool WarAffected { get; set; }

        [Column]
        public bool WarRelevantSystem { get; set; }

        [Column]
        public bool BarnacleMatrixInSystem { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        public List<StarSystemThargoidLevel>? ThargoidLevelHistory { get; set; }

        public List<StarSystemMinorFactionPresence>? MinorFactionPresences { get; set; }

        public IEnumerable<DcohFactionOperation>? FactionOperations { get; set; }

        public IEnumerable<Station>? Stations { get; set; }

        public IEnumerable<StarSystemFssSignal>? FssSignals { get; set; }

        public StarSystem(
            long id,
            long systemAddress,
            string name,
            decimal locationX,
            decimal locationY,
            decimal locationZ,
            long population,
            long originalPopulation,
            long populationMin,
            bool warAffected,
            bool warRelevantSystem,
            bool barnacleMatrixInSystem,
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
            OriginalPopulation = originalPopulation;
            PopulationMin = populationMin;
            WarAffected = warAffected;
            WarRelevantSystem = warRelevantSystem;
            BarnacleMatrixInSystem = barnacleMatrixInSystem;
            Created = created;
            Updated = updated;
        }

        [NotMapped]
        public bool RefreshedWarRelevantSystem => Population > 0 && DistanceTo(Vector3.Zero) <= 1000f;

        public void UpdateWarRelevantSystem()
        {
            WarRelevantSystem = RefreshedWarRelevantSystem;
        }

        public float DistanceTo(StarSystem system) => DistanceTo((float)system.LocationX, (float)system.LocationY, (float)system.LocationZ);

        public float DistanceTo(float x, float y, float z) => DistanceTo(new Vector3(x, y, z));

        public float DistanceTo(Vector3 location)
        {
            Vector3 system2 = new((float)LocationX, (float)LocationY, (float)LocationZ);
            return Vector3.Distance(location, system2);
        }
    }
}
