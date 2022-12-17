namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetailSystemAtRisk
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public long Population { get; set; }
        public OverwatchMaelstromDetailSystemAtRisk(string name, double distance, long population)
        {
            Name = name;
            Distance = distance;
            Population = population;
        }
    }
}
