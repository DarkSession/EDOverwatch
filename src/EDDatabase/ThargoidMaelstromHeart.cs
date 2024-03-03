namespace EDDatabase
{
    [Table("ThargoidMaelstromHeart")]
    public class ThargoidMaelstromHeart
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public short Heart { get; set; }

        [ForeignKey("MaelstromId")]
        public ThargoidMaelstrom? Maelstrom { get; set; }

        [Column]
        public DateTimeOffset? DestructionTime { get; set; }

        public ThargoidMaelstromHeart(int id, short heart, DateTimeOffset? destructionTime)
        {
            Id = id;
            Heart = heart;
            DestructionTime = destructionTime;
        }
    }
}
