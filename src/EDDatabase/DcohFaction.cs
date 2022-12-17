namespace EDDatabase
{
    [Table("DcohFaction")]
    public class DcohFaction
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(8)")]
        public string Short { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public ulong CreatedBy { get; set; }

        public DcohFaction(int id, string name, string @short, DateTimeOffset created, ulong createdBy)
        {
            Id = id;
            Name = name;
            Short = @short;
            Created = created;
            CreatedBy = createdBy;
        }
    }
}
