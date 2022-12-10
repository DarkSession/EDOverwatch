namespace EDOverwatch_Web.Models
{
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
}
