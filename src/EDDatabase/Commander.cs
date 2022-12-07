namespace EDDatabase
{
    [Table("Commander")]
    [Index(nameof(FDevCustomerId), IsUnique = true)]
    public class Commander
    {
        [Column]
        public int Id { get; set; }

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
        public DateTimeOffset LogLastDateProcessed { get; set; }

        [Column]
        public int LogLastLine { get; set; }

        [Column]
        public DateTime OAuthCreated { get; set; }

        [Column(TypeName = "varchar(4096)")]
        public string OAuthAccessToken { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string OAuthRefreshToken { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string OAuthTokenType { get; set; }

        public Commander(int id, long fDevCustomerId, bool isInLiveVersion, DateTimeOffset logLastDateProcessed, int logLastLine, DateTime oAuthCreated, string oAuthAccessToken, string oAuthRefreshToken, string oAuthTokenType)
        {
            Id = id;
            FDevCustomerId = fDevCustomerId;
            IsInLiveVersion = isInLiveVersion;
            LogLastDateProcessed = logLastDateProcessed;
            LogLastLine = logLastLine;
            OAuthCreated = oAuthCreated;
            OAuthAccessToken = oAuthAccessToken;
            OAuthRefreshToken = oAuthRefreshToken;
            OAuthTokenType = oAuthTokenType;
        }
    }
}
