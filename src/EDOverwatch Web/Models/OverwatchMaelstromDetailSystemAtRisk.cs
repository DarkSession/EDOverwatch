namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetailSystemAtRisk
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public long Population { get; set; }
        public List<OverwatchStarSystemMin> AttackingSystems { get; set; }

        public OverwatchMaelstromDetailSystemAtRisk(string name, double distance, long population, List<StarSystem> attackingSystems)
        {
            Name = name;
            Distance = distance;
            Population = population;
            AttackingSystems = attackingSystems.Select(a => new OverwatchStarSystemMin(a)).ToList();
        }
    }
}
