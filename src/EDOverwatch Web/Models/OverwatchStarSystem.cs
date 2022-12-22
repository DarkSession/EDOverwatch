namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem
    {
        public long SystemAddress { get; set; }
        public string Name { get; }
        public OverwatchStarSystemCoordinates Coordinates { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal? ProgressPercent { get; }
        public decimal EffortFocus { get; }
        public int FactionOperations { get; protected set; }
        public List<OverwatchStarSystemSpecialFactionOperation> SpecialFactionOperations { get; }
        public int StationsUnderRepair { get; protected set; }
        public int StationsDamaged { get; protected set; }
        public int StationsUnderAttack { get; protected set; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus, int factionOperations, List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations, int stationsUnderRepair, int stationsDamaged, int stationsUnderAttack)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
            Coordinates = new(starSystem.LocationX, starSystem.LocationY, starSystem.LocationZ);
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            ProgressPercent = (Progress != null) ? (decimal)Progress / 100m : null;
            EffortFocus = effortFocus;
            FactionOperations = factionOperations;
            SpecialFactionOperations = specialFactionOperations;
            StationsUnderRepair = stationsUnderRepair;
            StationsDamaged = stationsDamaged;
            StationsUnderAttack = stationsUnderAttack;
        }
    }

    public class OverwatchStarSystemSpecialFactionOperation
    {
        public string Tag { get; }
        public string Name { get; }
        public OverwatchStarSystemSpecialFactionOperation(string tag, string name)
        {
            Tag = tag;
            Name = name;
        }
    }

    public class OverwatchStarSystemCoordinates
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }
        public OverwatchStarSystemCoordinates(decimal x, decimal y, decimal z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
