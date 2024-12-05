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
        public OverwatchRescueShip? RescueShip { get; }
        public string? BodyName { get; }

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
            if (station.Body?.Gravity is decimal gravity)
            {
                Gravity = Math.Round(gravity / 0.980665m, 4);
            }
            OdysseyOnly = (station.Body?.OdysseyOnly == true);
            if (rescueShips.Any())
            {
                MinorFaction? minorFaction = station.PriorMinorFaction ?? station.MinorFaction;
                if (minorFaction?.Allegiance != null)
                {
                    var rescueShip = rescueShips.Where(r =>
                        r.StarSystem != null &&
                        r.RescueAllegianceId == minorFaction.Allegiance.Id)
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
            BodyName = station.Body?.Name.Replace(station.StarSystem?.Name ?? string.Empty, string.Empty);
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
}
