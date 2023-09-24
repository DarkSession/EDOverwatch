namespace EDDatabase
{
    [Table("StarSystemFssSignal")]
    public class StarSystemFssSignal
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column(TypeName = "varchar(512)")]
        public string Name { get; set; }

        [Column]
        public StarSystemFssSignalType Type { get; set; }

        [Column]
        public DateTimeOffset FirstSeen { get; set; }

        [Column]
        public DateTimeOffset LastSeen { get; set; }

        public StarSystemFssSignal(int id, string name, StarSystemFssSignalType type, DateTimeOffset firstSeen, DateTimeOffset lastSeen)
        {
            Id = id;
            Name = name;
            Type = type;
            FirstSeen = firstSeen;
            LastSeen = lastSeen;
        }
    }

    public enum StarSystemFssSignalType : byte
    {
        Other = 0,
        FleetCarrier,
        Titan,
        AXCZ,
        ThargoidActivity,
    }
}
