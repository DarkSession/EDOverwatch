namespace EDDatabase
{
    [Table("ThargoidCycle")]
    public class ThargoidCycle
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public DateTimeOffset Start { get; set; }

        [Column]
        public DateTimeOffset End { get; set; }

        public ThargoidCycle(int id, DateTimeOffset start, DateTimeOffset end)
        {
            Id = id;
            Start = start;
            End = end;
        }

        [NotMapped]
        public bool IsCurrent => Start <= DateTimeOffset.UtcNow && End >= DateTimeOffset.UtcNow;
    }
}
