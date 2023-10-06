namespace EDDatabase
{
    [Table("Commodity")]
    public class Commodity
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string? NameEnglish { get; set; }

        public Commodity(int id, string name, string? nameEnglish)
        {
            Id = id;
            Name = name;
            NameEnglish = nameEnglish;
        }

        public static async Task<Commodity> GetCommodity(string name, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            name = name.Trim().ToLower();
            Commodity? commodity = await dbContext.Commodities.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
            if (commodity == null)
            {
                commodity = new(0, name, null);
                dbContext.Commodities.Add(commodity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return commodity;
        }
    }
}
