namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem
    {
        public long SystemAddress { get; set; }
        public string Name { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal? ProgressPercent { get; }
        public decimal EffortFocus { get; }
        public int FactionOperations { get; protected set; }
        public List<OverwatchStarSystemSpecialFactionOperation> SpecialFactionOperations { get; }
        public int StationsUnderRepair { get; }
        public int StationsDamaged { get; }
        public int StationsUnderAttack { get; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus, int factionOperations, List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations, int stationsUnderRepair, int stationsDamaged, int stationsUnderAttack)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            ProgressPercent = (Progress != null) ? Progress / 100 : null;
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
}
