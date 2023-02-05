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

        public OverwatchStation(Station station)
        {
            Name = station.Name;
            MarketId = station.MarketId;
            DistanceFromStarLS = station.DistanceFromStarLS;
            Type = station.Type?.Name ?? throw new Exception("station.Type cannot be null");
            LandingPads = new(station.LandingPadSmall, station.LandingPadMedium, station.LandingPadLarge);
            State = station.State.GetEnumMemberValue();
            Gravity = station.Body?.Gravity;
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
