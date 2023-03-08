namespace EDDatabase
{
    [Table("MinorFaction")]
    [Index(nameof(Name), IsUnique = true)]
    public class MinorFaction
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [ForeignKey("AllegianceId")]
        public FactionAllegiance? Allegiance { get; set; }

        public MinorFaction(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static async Task<MinorFaction> GetByName(string name, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            MinorFaction? minorFaction = await dbContext.MinorFactions
                .Include(m => m.Allegiance)
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
            if (minorFaction == null)
            {
                minorFaction = new(0, name);
                dbContext.MinorFactions.Add(minorFaction);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return minorFaction;
        }
    }
}
