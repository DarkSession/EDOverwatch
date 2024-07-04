namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionSystem
    {
        public OverwatchStarSystemMin StarSystem { get; set; }
        public double Distance { get; set; }
        public List<OverwatchAlertPredictionSystemAttacker> Attackers { get; set; }
        public bool PrimaryTarget { get; set; }
        public bool IsActive { get; set; }
        public int Order { get; set; }
        public bool SpireSite { get; set; }
        public bool InvasionPredicted { get; set; }

        public OverwatchAlertPredictionSystem(StarSystem starSystem, ThargoidMaelstrom maelstrom, double distance, AlertPrediction alertPrediction, bool invasionPredicted)
        {
            StarSystem = new(starSystem);
            Distance = distance;
            Attackers = alertPrediction.Attackers!
                .OrderBy(a => a.Order)
                .Select(a =>
                {
                    double distance = Math.Round(maelstrom.StarSystem!.DistanceTo(a.StarSystem!), 4);
                    return new OverwatchAlertPredictionSystemAttacker(a, distance);
                }).ToList();
            PrimaryTarget = alertPrediction.AlertLikely;
            IsActive = alertPrediction.Status == AlertPredictionStatus.Default;
            Order = alertPrediction.Order;
            SpireSite = starSystem.BarnacleMatrixInSystem;
            InvasionPredicted = invasionPredicted;
        }
    }
}
