namespace EDOverwatch_Web.Models
{
    public abstract class OverwatchStarSystemBase
    {
        public long SystemAddress { get; set; }
        public string Name { get; }
        public OverwatchStarSystemCoordinates Coordinates { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public long Population { get; }

        public OverwatchStarSystemBase(StarSystem starSystem)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
            Coordinates = new(starSystem.LocationX, starSystem.LocationY, starSystem.LocationZ);
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            Population = starSystem.Population;
        }
    }
}
