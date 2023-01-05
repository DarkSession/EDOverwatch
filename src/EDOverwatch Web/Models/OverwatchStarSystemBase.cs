namespace EDOverwatch_Web.Models
{
    public abstract class OverwatchStarSystemBase : OverwatchStarSystemMin
    {
        public OverwatchStarSystemCoordinates Coordinates { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public long Population { get; }

        public OverwatchStarSystemBase(StarSystem starSystem) : base(starSystem)
        {
            Coordinates = new(starSystem.LocationX, starSystem.LocationY, starSystem.LocationZ);
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            Population = starSystem.Population;
        }
    }

    public class OverwatchStarSystemMin
    {
        public long SystemAddress { get; set; }
        public string Name { get; }

        public OverwatchStarSystemMin(StarSystem starSystem)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
        }
    }
}
