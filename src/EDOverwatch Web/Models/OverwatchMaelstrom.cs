namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstrom
    {
        public string Name { get; }
        public string SystemName { get; }
        public long SystemAddress { get; }
        public short IngameNumber { get; }
        public short HeartsRemaining { get; }

        public OverwatchMaelstrom(ThargoidMaelstrom thargoidMaelstrom)
        {
            Name = thargoidMaelstrom.Name;
            SystemName = thargoidMaelstrom.StarSystem?.Name ?? string.Empty;
            SystemAddress = thargoidMaelstrom.StarSystem?.SystemAddress ?? 0;
            IngameNumber = thargoidMaelstrom.IngameNumber;
            HeartsRemaining = thargoidMaelstrom.HeartsRemaining;
        }
    }
}
