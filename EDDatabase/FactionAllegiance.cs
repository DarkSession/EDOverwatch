namespace EDDatabase
{
    [Table("FactionAllegiance")]
    [Index(nameof(Name), IsUnique = true)]
    public class FactionAllegiance
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        public FactionAllegiance(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static async Task<FactionAllegiance> GetByName(string name, EdDbContext dbContext)
        {
            FactionAllegiance? allegiance = await dbContext.FactionAllegiances.SingleOrDefaultAsync(c => c.Name == name);
            if (allegiance == null)
            {
                allegiance = new FactionAllegiance(0, name);
                dbContext.FactionAllegiances.Add(allegiance);
                await dbContext.SaveChangesAsync();
            }
            return allegiance;
        }
    }
}
