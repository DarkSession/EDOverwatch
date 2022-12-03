namespace EDDatabase
{
    [Table("ThargoidMaelstrom")]
    public class ThargoidMaelstrom
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        public ThargoidMaelstrom(int id, string name, DateTimeOffset updated)
        {
            Id = id;
            Name = name;
            Updated = updated;
        }
    }
}
