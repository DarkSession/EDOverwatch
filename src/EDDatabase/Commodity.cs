namespace EDDatabase
{
    [Table("Commodity")]
    public class Commodity
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        public Commodity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static async Task<Commodity> GetCommodity(string name, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            name = name.Trim().ToLower();
            Commodity? commodity = await dbContext.Commodities.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
            if (commodity == null)
            {
                commodity = new(0, name);
                dbContext.Commodities.Add(commodity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return commodity;
        }

        public static List<string> ThargoidWarRelatedCommodities { get; } = new()
        {
            "UnknownArtifact2",
            "ThargoidTissueSampleType1 ",
            "ThargoidTissueSampleType2",
            "ThargoidTissueSampleType3",
            "ThargoidTissueSampleType4",
            "ThargoidTissueSampleType5",
            "ThargoidScoutTissueSample",
            "USSCargoBlackBox",
            "OccupiedCryoPod",
            "DamagedEscapePod",
            "WreckageComponents",
            "ThargoidTissueSampleType6",
            "ThargoidTissueSampleType9a",
            "ThargoidTissueSampleType9b",
            "ThargoidTissueSampleType9c",
            "ThargoidTissueSampleType10a",
            "ThargoidTissueSampleType10b",
            "ThargoidTissueSampleType10c",
        };
    }
}
