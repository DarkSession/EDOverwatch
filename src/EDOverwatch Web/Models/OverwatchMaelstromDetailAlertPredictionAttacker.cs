namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetailAlertPredictionAttacker
    {
        public OverwatchStarSystem StarSystem { get; set; }
        public double Distance { get; set; }

        public OverwatchMaelstromDetailAlertPredictionAttacker(AlertPredictionAttacker alertPredictionAttacker, double distance)
        {
            StarSystem = new(alertPredictionAttacker.StarSystem!, 0, 0, 0, 0, 0, new(), 0, 0, 0);
            Distance = distance;
        }
    }
}
