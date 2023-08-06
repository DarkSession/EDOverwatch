namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetailAlertPrediction
    {
        public OverwatchStarSystemMin StarSystem { get; set; }
        public double Distance { get; set; }
        public List<OverwatchMaelstromDetailAlertPredictionAttacker> Attackers { get; set; }
        public bool PrimaryTarget { get; set; }

        public OverwatchMaelstromDetailAlertPrediction(StarSystem starSystem, ThargoidMaelstrom maelstrom, double distance, List<AlertPredictionAttacker> attackers, bool primaryTarget)
        {
            StarSystem = new(starSystem);
            Distance = distance;
            Attackers = attackers
                .OrderBy(a => a.Order)
                .Select(a =>
                {
                    double distance = Math.Round(maelstrom.StarSystem!.DistanceTo(a.StarSystem!), 2);
                    return new OverwatchMaelstromDetailAlertPredictionAttacker(a, distance);
                }).ToList();
            PrimaryTarget = primaryTarget;
        }
    }
}
