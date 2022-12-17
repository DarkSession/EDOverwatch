namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem
    {
        public long SystemAddress { get; set; }
        public string Name { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal EffortFocus { get; }
        public int FactionOperations { get; protected set; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus, int factionOperations)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            EffortFocus = effortFocus;
            FactionOperations = factionOperations;
        }
    }
}
