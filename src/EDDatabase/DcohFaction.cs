namespace EDDatabase
{
    [Table("DcohFaction")]
    [Index(nameof(Short), IsUnique = true)]
    public class DcohFaction
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column(TypeName = "varchar(8)")]
        public string Short { get; set; }

        [Column]
        public bool SpecialFaction { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [ForeignKey("CreatedById")]
        public DcohDiscordUser? CreatedBy { get; set; }

        public DcohFaction(int id, string name, string @short, bool specialFaction, DateTimeOffset created)
        {
            Id = id;
            Name = name;
            Short = @short;
            SpecialFaction = specialFaction;
            Created = created;
        }
    }
}
