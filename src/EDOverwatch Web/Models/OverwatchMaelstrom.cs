namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstrom
    {
        public string Name { get; }
        public string SystemName { get; }
        public long SystemAddress { get; }
        [Obsolete("No longer exists in-game")]
        public short IngameNumber { get; }

        public OverwatchMaelstrom(ThargoidMaelstrom thargoidMaelstrom)
        {
            Name = thargoidMaelstrom.Name;
            SystemName = thargoidMaelstrom.StarSystem?.Name ?? string.Empty;
            SystemAddress = thargoidMaelstrom.StarSystem?.SystemAddress ?? 0;
#pragma warning disable CS0618 // Type or member is obsolete
            IngameNumber = thargoidMaelstrom.IngameNumber;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
