namespace EDDatabase
{
    [Table("ApiKey")]
    [Index(nameof(Key), IsUnique = true)]
    public class ApiKey
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public Guid Key { get; set; }

        [Column]
        public bool DataUpdate { get; set; }

        [Column]
        public bool FactionUpdate { get; set; }

        [Column(TypeName = "varchar(8)")]
        public string? Faction { get; set; }

        public ApiKey(int id, Guid key, bool dataUpdate, bool factionUpdate, string? faction)
        {
            Id = id;
            Key = key;
            DataUpdate = dataUpdate;
            FactionUpdate = factionUpdate;
            Faction = faction;
        }
    }
}
