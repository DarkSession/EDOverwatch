namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictionSystemAttacker
    {
        public OverwatchStarSystem StarSystem { get; set; }
        public double Distance { get; set; }

        public OverwatchAlertPredictionSystemAttacker(AlertPredictionAttacker alertPredictionAttacker, double distance)
        {
            StarSystem = new(alertPredictionAttacker.StarSystem!);
            Distance = distance;
        }
    }
}
