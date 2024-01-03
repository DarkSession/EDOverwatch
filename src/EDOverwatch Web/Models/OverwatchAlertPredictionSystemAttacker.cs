namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionSystemAttacker
    {
        public OverwatchStarSystem StarSystem { get; set; }
        public double Distance { get; set; }
        public bool IsActive { get; set; }

        public OverwatchAlertPredictionSystemAttacker(AlertPredictionAttacker alertPredictionAttacker, double distance)
        {
            StarSystem = new(alertPredictionAttacker.StarSystem!, false);
            Distance = distance;
            IsActive = alertPredictionAttacker.Status == AlertPredictionAttackerStatus.Default;
        }
    }
}
