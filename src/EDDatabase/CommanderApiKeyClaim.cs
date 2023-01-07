namespace EDDatabase
{
    [Table("CommanderApiKeyClaim")]
    public class CommanderApiKeyClaim
    {
        [Column]
        public int Id { get; set; }

        [ForeignKey("CommanderId")]
        public Commander? Commander { get; set; }

        [ForeignKey("ApiKeyId")]
        public CommanderApiKey? ApiKey { get; set; }

        public CommanderApiKeyClaim(int id)
        {
            Id = id;
        }
    }
}
