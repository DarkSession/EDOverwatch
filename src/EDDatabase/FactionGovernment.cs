using System.Text.RegularExpressions;

namespace EDDatabase
{
    [Table("FactionGovernment")]
    [Index(nameof(Name), IsUnique = true)]
    public partial class FactionGovernment
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        public FactionGovernment(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [GeneratedRegex("\\$government_([A-Z]+);", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex GovernmentRegex();

        [NotMapped]
        private static Regex Regex { get; } = GovernmentRegex();

        private static string GetInternalString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            Match match = Regex.Match(s);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return s;
        }

        public static async Task<FactionGovernment?> GetByName(string input, EdDbContext dbContext, CancellationToken cancellationToken = default)
        {
            string name = GetInternalString(input);
            if (!string.IsNullOrEmpty(name))
            {
                FactionGovernment? government = await dbContext.FactionGovernments.SingleOrDefaultAsync(c => c.Name == name, cancellationToken);
                if (government == null)
                {
                    government = new FactionGovernment(0, name);
                    dbContext.FactionGovernments.Add(government);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                return government;
            }
            return null;
        }
    }
}
