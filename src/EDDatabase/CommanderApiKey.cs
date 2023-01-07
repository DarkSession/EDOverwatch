namespace EDDatabase
{
    [Table("CommanderApiKey")]
    [Index(nameof(Key), IsUnique = true)]
    public class CommanderApiKey
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public Guid Key { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public CommanderApiKeyStatus Status { get; set; }

        public CommanderApiKey(int id, Guid key, DateTimeOffset created, CommanderApiKeyStatus status)
        {
            Id = id;
            Key = key;
            Created = created;
            Status = status;
        }
    }

    public enum CommanderApiKeyStatus : byte
    {
        Inactive,
        Active,
    }
}
