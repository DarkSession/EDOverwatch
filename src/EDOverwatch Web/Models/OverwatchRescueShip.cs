namespace EDOverwatch_Web.Models
{
    public class OverwatchRescueShip
    {
        public string Name { get; set; }
        public OverwatchStarSystemMin System { get; set; }
        public decimal DistanceLy { get; set; }

        public OverwatchRescueShip(string name, StarSystem starSystem, decimal distanceLy)
        {
            Name = name;
            System = new(starSystem);
            DistanceLy = Math.Round(distanceLy, 4);
        }
    }
}
