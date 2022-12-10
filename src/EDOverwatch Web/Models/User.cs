namespace EDOverwatch_Web.Models
{
    public class User
    {
        public string Commander { get; set; }
        public DateTimeOffset? JournalLastImport { get; set; }
        public User(string commander, DateTimeOffset? journalLastImport)
        {
            Commander = commander;
            JournalLastImport = journalLastImport;
        }
    }
}
