namespace EDOverwatch_Web.Models
{
    public class User
    {
        public string Commander { get; set; }
        public bool HasActiveToken { get; set; }
        public DateTimeOffset? JournalLastImport { get; set; }
        public User(Commander commander)
        {
            Commander = commander.Name ?? commander.User?.UserName ?? "Unknown";
            HasActiveToken = commander.OAuthStatus == CommanderOAuthStatus.Active;
            JournalLastImport = commander.JournalLastActivity;
        }
    }
}
