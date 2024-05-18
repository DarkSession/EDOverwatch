namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionMaelstrom
    {
        public OverwatchMaelstrom Maelstrom { get; set; }
        public List<OverwatchAlertPredictionSystem> Systems { get; set; }
        public List<OverwatchAlertPredictionMaelstromAttackerCount> AttackingSystemCount { get; }
        public int ExpectedAlerts { get; }

        public OverwatchAlertPredictionMaelstrom(ThargoidMaelstrom thargoidMaelstrom, List<AlertPrediction> alertPredictions)
        {
            Maelstrom = new(thargoidMaelstrom);
#pragma warning disable IDE0305 // Simplify collection initialization
            Systems = alertPredictions
                .Select(a =>
                {
                    double distance = Math.Round(a.StarSystem!.DistanceTo(thargoidMaelstrom.StarSystem!), 4);
                    return new OverwatchAlertPredictionSystem(a.StarSystem, thargoidMaelstrom, distance, a);
                })
                .OrderBy(a => a.Order)
                .ThenBy(a => a.Distance)
                .ToList();
            AttackingSystemCount = alertPredictions
                .SelectMany(a => a.Attackers!)
                .GroupBy(a => a.StarSystemId)
                .Select(a => new OverwatchAlertPredictionMaelstromAttackerCount(a.First().StarSystem!, a.Count()))
                .OrderByDescending(a => a.Count)
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
            ExpectedAlerts = alertPredictions.Count(a => a.AlertLikely);
        }
    }

    public class OverwatchAlertPredictionMaelstromAttackerCount
    {
        public OverwatchStarSystem StarSystem { get; }
        public int Count { get; }

        public OverwatchAlertPredictionMaelstromAttackerCount(StarSystem starSystem, int count)
        {
            StarSystem = new(starSystem, false);
            Count = count;
        }
    }
}
