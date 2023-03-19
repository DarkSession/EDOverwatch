namespace EDOverwatch_Web.Models
{
    public class OverwatchStation
    {
        public string Name { get; }
        public long MarketId { get; }
        public decimal DistanceFromStarLS { get; }
        public string Type { get; }
        public OverwatchStationLandingPads LandingPads { get; }
        public string State { get; }
        public decimal? Gravity { get; }
        public bool OdysseyOnly { get; }
        public OverwatchStationRescueShip? RescueShip { get; }

        public OverwatchStation(Station station, List<Station> rescueShips, StarSystemThargoidLevel thargoidLevel)
        {
            Name = station.Name;
            MarketId = station.MarketId;
            DistanceFromStarLS = station.DistanceFromStarLS;
            Type = station.Type?.Name ?? throw new Exception("station.Type cannot be null");
            LandingPads = new(station.LandingPadSmall, station.LandingPadMedium, station.LandingPadLarge);
            if (station.State != StationState.Normal || thargoidLevel.State != StarSystemThargoidLevelState.Invasion || station.Updated >= WeeklyTick.GetLastTick())
            {
                State = station.State.GetEnumMemberValue();
            }
            else
            {
                State = "Unknown";
            }
            if (station.Body?.Gravity != null)
            {
                Gravity = station.Body.Gravity / 0.980665m;
            }
            OdysseyOnly = (station.Body?.OdysseyOnly == true);
            if (rescueShips.Any())
            {
                MinorFaction? minorFaction = station.PriorMinorFaction ?? station.MinorFaction;
                if (minorFaction?.Allegiance != null)
                {
                    var rescueShip = rescueShips.Where(r =>
                        r.StarSystem != null &&
                        r.MinorFaction?.Allegiance != null &&
                        r.MinorFaction?.Allegiance?.Name == minorFaction.Allegiance.Name)
                        .Select(r => new
                        {
                            RescueShip = r,
                            Distance = r.StarSystem!.DistanceTo(station.StarSystem!),
                        })
                        .OrderBy(r => r.Distance)
                        .FirstOrDefault();
                    if (rescueShip != null)
                    {
                        RescueShip = new(rescueShip.RescueShip.Name, rescueShip.RescueShip.StarSystem!, (decimal)rescueShip.Distance);
                    }
                }
            }
        }
    }

    public class OverwatchStationLandingPads
    {
        public short Small { get; }
        public short Medium { get; }
        public short Large { get; }
        public OverwatchStationLandingPads(short small, short medium, short large)
        {
            Small = small;
            Medium = medium;
            Large = large;
        }
    }

    public class OverwatchStationRescueShip
    {
        public string Name { get; set; }
        public OverwatchStarSystemMin System { get; set; }
        public decimal DistanceLy { get; set; }

        public OverwatchStationRescueShip(string name, StarSystem starSystem, decimal distanceLy)
        {
            Name = name;
            System = new(starSystem);
            DistanceLy = distanceLy;
        }
    }
}
