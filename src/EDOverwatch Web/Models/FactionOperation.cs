namespace EDOverwatch_Web.Models
{
    public class FactionOperation
    {
        public string Faction { get; set; }
        public string Type { get; set; }
        public DateTimeOffset Started { get; set; }
        public string SystemName { get; set; }
        public long SystemAddress { get; set; }
        public string? MeetingPoint { get; set; }

        public FactionOperation(DcohFactionOperation factionOperation)
        {
            if (factionOperation.Faction == null)
            {
                throw new Exception("Faction cannot be null");
            }
            else if (factionOperation.StarSystem == null)
            {
                throw new Exception("StarSystem cannot be null");
            }
            Faction = $"{factionOperation.Faction.Name} ({factionOperation.Faction.Short})";
            Type = factionOperation.Type.GetEnumMemberValue();
            Started = factionOperation.Created;
            SystemName = factionOperation.StarSystem.Name;
            SystemAddress = factionOperation.StarSystem.SystemAddress;
            MeetingPoint = factionOperation.MeetingPoint;
        }
    }
}
