using System.Text.RegularExpressions;

namespace EDDatabase
{
    [Table("Economy")]
    [Index(nameof(Name), IsUnique = true)]
    public partial class Economy
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        public Economy(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [GeneratedRegex("\\$economy_([A-Z]+);", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex EconomyRegex();

        [NotMapped]
        private static Regex Regex { get; } = EconomyRegex();

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

        public static async Task<Economy?> GetByName(string input, EdDbContext dbContext, CancellationToken cancellationToken = default)
        {
            string name = GetInternalString(input);
            if (!string.IsNullOrEmpty(name))
            {
                Economy? economy = await dbContext.Economies.SingleOrDefaultAsync(c => c.Name == name, cancellationToken);
                if (economy == null)
                {
                    economy = new Economy(0, name);
                    dbContext.Economies.Add(economy);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                return economy;
            }
            return null;
        }
    }
}
