using EDUtils;

namespace EDOverwatch_Web.Models
{
    public class OverwatchSystems
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>().Select(s => new OverwatchThargoidLevel(s)).ToList();
        public List<OverwatchStarSystem> Systems { get; } = new();

        public OverwatchSystems(List<ThargoidMaelstrom> thargoidMaelstroms)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
        }
    }

    public class OverwatchMaelstrom
    {
        public string Name { get; }
        public string SystemName { get; }

        public OverwatchMaelstrom(ThargoidMaelstrom thargoidMaelstrom)
        {
            Name = thargoidMaelstrom.Name;
            SystemName = thargoidMaelstrom.StarSystem?.Name ?? string.Empty;
        }
    }

    public class OverwatchThargoidLevel
    {
        public StarSystemThargoidLevelState Level { get; }
        public string Name { get; }

        public OverwatchThargoidLevel(StarSystemThargoidLevelState level)
        {
            Level = level;
            Name = level.GetEnumMemberValue();
        }
    }

    public class OverwatchStarSystem
    {
        public string Name { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal EffortFocus { get; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus)
        {
            Name = starSystem.Name;
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            EffortFocus = effortFocus;
        }
    }
}
