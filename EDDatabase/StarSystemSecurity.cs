using System.Text.RegularExpressions;

namespace EDDatabase
{
    [Table("StarSystemSecurity")]
    [Index(nameof(Name), IsUnique = true)]
    public partial class StarSystemSecurity
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        public StarSystemSecurity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [GeneratedRegex("\\$SYSTEM_SECURITY_([A-Z]+);", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex SystemSecurityRegex();

        [NotMapped]
        private static Regex Regex { get; } = SystemSecurityRegex();

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

        public static async Task<StarSystemSecurity> GetByName(string input, EdDbContext dbContext)
        {
            string name = GetInternalString(input);
            StarSystemSecurity? starSystemSecurity = await dbContext.StarSystemSecurities.SingleOrDefaultAsync(c => c.Name == name);
            if (starSystemSecurity == null)
            {
                starSystemSecurity = new StarSystemSecurity(0, name);
                dbContext.StarSystemSecurities.Add(starSystemSecurity);
                await dbContext.SaveChangesAsync();
            }
            return starSystemSecurity;
        }
    }
}
