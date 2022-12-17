namespace EDDatabase
{
    [Table("DcohDiscordUser")]
    [Index(nameof(DiscordId), IsUnique = true)]
    public class DcohDiscordUser
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public ulong DiscordId { get; set; }

        [ForeignKey("FactionId")]
        public DcohFaction? Faction { get; set; }

        [Column]
        public DateTimeOffset FactionJoined { get; set; }

        public DcohDiscordUser(int id, ulong discordId, DateTimeOffset factionJoined)
        {
            Id = id;
            DiscordId = discordId;
            FactionJoined = factionJoined;
        }
    }
}
