namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionMaelstrom
    {
        public OverwatchMaelstrom Maelstrom { get; set; }
        public List<OverwatchAlertPredictionSystem> Systems { get; set; }

        public OverwatchAlertPredictionMaelstrom(ThargoidMaelstrom thargoidMaelstrom, List<AlertPrediction> alertPredictions)
        {
            Maelstrom = new(thargoidMaelstrom);
            Systems = alertPredictions.Select(a =>
            {
                double distance = Math.Round(a.StarSystem!.DistanceTo(thargoidMaelstrom.StarSystem!), 2);
                return new OverwatchAlertPredictionSystem(a.StarSystem, thargoidMaelstrom, distance, a.Attackers!, a.AlertLikely);
            }).ToList();
        }
    }
}
