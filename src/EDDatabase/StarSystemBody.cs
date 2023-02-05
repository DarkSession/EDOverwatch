namespace EDDatabase
{
    [Table("StarSystemBody")]
    public class StarSystemBody
    {
        [Column]
        public int Id { get; set; }

        [Column(TypeName = "varchar(512)")]
        public string Name { get; set; }

        [Column]
        public int BodyId { get; set; }

        [ForeignKey("StarSystemId")]
        public StarSystem? StarSystem { get; set; }

        [Column(TypeName = "decimal(14,8)")]
        public decimal? Gravity { get; set; }

        public StarSystemBody(int id, int bodyId, string name, decimal? gravity)
        {
            Id = id;
            BodyId = bodyId;
            Name = name;
            Gravity = gravity;
        }
    }
}
