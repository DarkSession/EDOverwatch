namespace EDDatabase
{
    [Table("ApiKey")]
    [Index(nameof(Key), IsUnique = true)]
    public class ApiKey
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public Guid Key { get; set; }

        public ApiKey(int id, Guid key)
        {
            Id = id;
            Key = key;
        }
    }
}
