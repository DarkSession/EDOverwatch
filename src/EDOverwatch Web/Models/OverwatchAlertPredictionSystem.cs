namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionSystem
    {
        public OverwatchStarSystemMin StarSystem { get; set; }
        public double Distance { get; set; }
        public List<OverwatchAlertPredictionSystemAttacker> Attackers { get; set; }
        public bool PrimaryTarget { get; set; }

        public OverwatchAlertPredictionSystem(StarSystem starSystem, ThargoidMaelstrom maelstrom, double distance, List<AlertPredictionAttacker> attackers, bool primaryTarget)
        {
            StarSystem = new(starSystem);
            Distance = distance;
            Attackers = attackers
                .OrderBy(a => a.Order)
                .Select(a =>
                {
                    double distance = Math.Round(maelstrom.StarSystem!.DistanceTo(a.StarSystem!), 2);
                    return new OverwatchAlertPredictionSystemAttacker(a, distance);
                }).ToList();
            PrimaryTarget = primaryTarget;
        }
    }
}
