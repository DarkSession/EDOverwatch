namespace EDDatabase
{
    [Table("Commander")]
    [Index(nameof(FDevCustomerId), IsUnique = true)]
    public class Commander
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column]
        public long FDevCustomerId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("SystemId")]
        public StarSystem? System { get; set; }

        [ForeignKey("StationId")]
        public Station? Station { get; set; }

        [Column]
        public bool IsInLiveVersion { get; set; }

        [Column]
        public DateTimeOffset JournalLastProcessed { get; set; }

        [Column]
        public DateOnly JournalDay { get; set; }

        [Column]
        public int JournalLastLine { get; set; }

        [Column]
        public DateTimeOffset JournalLastActivity { get; set; }

        [Column]
        public DateTimeOffset OAuthCreated { get; set; }

        [Column]
        public CommanderOAuthStatus OAuthStatus { get; set; }

        [Column(TypeName = "varchar(4096)")]
        public string OAuthAccessToken { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string OAuthRefreshToken { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string OAuthTokenType { get; set; }

        [NotMapped]
        public bool CanProcessCApiJournal => OAuthStatus == CommanderOAuthStatus.Active && JournalLastProcessed < DateTimeOffset.Now.AddMinutes(-5);

        public Commander(int id, string name, long fDevCustomerId, bool isInLiveVersion, DateTimeOffset journalLastProcessed, DateOnly journalDay, int journalLastLine, DateTimeOffset journalLastActivity, DateTimeOffset oAuthCreated, CommanderOAuthStatus oAuthStatus, string oAuthAccessToken, string oAuthRefreshToken, string oAuthTokenType)
        {
            Id = id;
            Name = name;
            FDevCustomerId = fDevCustomerId;
            IsInLiveVersion = isInLiveVersion;
            JournalLastProcessed = journalLastProcessed;
            JournalDay = journalDay;
            JournalLastLine = journalLastLine;
            JournalLastActivity = journalLastActivity;
            OAuthCreated = oAuthCreated;
            OAuthStatus = oAuthStatus;
            OAuthAccessToken = oAuthAccessToken;
            OAuthRefreshToken = oAuthRefreshToken;
            OAuthTokenType = oAuthTokenType;
        }
    }

    public enum CommanderOAuthStatus : byte
    {
        Inactive = 0,
        Active,
        Expires,
    }
}
