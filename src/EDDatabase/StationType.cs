namespace EDDatabase
{
    [Table("StationType")]
    [Index(nameof(Name), IsUnique = true)]
    public class StationType
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string NameEnglish { get; set; }

        public StationType(int id, string name, string nameEnglish)
        {
            Id = id;
            Name = name;
            NameEnglish = nameEnglish;
        }

        public static async Task<StationType> GetByName(string name, EdDbContext dbContext, CancellationToken cancellationToken = default)
        {
            StationType? stationType = await dbContext.StationTypes.SingleOrDefaultAsync(c => c.Name == name, cancellationToken);
            if (stationType == null)
            {
                stationType = new StationType(0, name, name);
                dbContext.StationTypes.Add(stationType);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return stationType;
        }

        public static Task<StationType> GetFleetCarrier(EdDbContext dbContext, CancellationToken cancellationToken = default) => GetByName("FleetCarrier", dbContext, cancellationToken);

        [NotMapped]
        public string NameFull => !string.IsNullOrEmpty(NameEnglish) ? NameEnglish : Name;

        [NotMapped]
        public bool IsFleetCarrier => Name == "FleetCarrier";
    }
}
