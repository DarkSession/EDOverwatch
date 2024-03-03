namespace EDOverwatch_Web.Models
{
    public class FactionOperationStarSystemLevel : FactionOperation
    {
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public OverwatchMaelstrom Maelstrom { get; }

        public FactionOperationStarSystemLevel(DcohFactionOperation factionOperation) :
            base(factionOperation)
        {
            ThargoidLevel = new(factionOperation.StarSystem?.ThargoidLevel ?? new StarSystemThargoidLevel(0, StarSystemThargoidLevelState.None, 0, DateTimeOffset.UtcNow, false, false));
            Maelstrom = new(factionOperation.StarSystem?.ThargoidLevel?.Maelstrom ?? new ThargoidMaelstrom(0, string.Empty, 0m, 0, DateTimeOffset.UtcNow, 8, null));
        }
    }
}
