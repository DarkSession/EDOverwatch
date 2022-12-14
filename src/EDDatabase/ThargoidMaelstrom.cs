namespace EDDatabase
{
    [Table("ThargoidMaelstrom")]
    public class ThargoidMaelstrom
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string Name { get; set; }

        [Column(TypeName = "decimal(14,6)")]
        public decimal InfluenceSphere { get; set; }

        [Column]
        public short IngameNumber { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        public ThargoidMaelstrom(int id, string name, decimal influenceSphere, short ingameNumber, DateTimeOffset updated)
        {
            Id = id;
            Name = name;
            InfluenceSphere = influenceSphere;
            IngameNumber = ingameNumber;
            Updated = updated;
        }
    }
}
