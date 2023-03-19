namespace EDDatabase
{
    [Table("StarSystemMinorFactionPresence")]
    public class StarSystemMinorFactionPresence
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("MinorFactionId")]
        public MinorFaction? MinorFaction { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }


        public StarSystemMinorFactionPresence(int id)
        {
            Id = id;
        }
    }
}
