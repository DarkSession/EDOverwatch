namespace EDDatabase
{
    [Table("StarSystemThargoidLevelProgress")]
    public class StarSystemThargoidLevelProgress
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        public StarSystemThargoidLevel? ThargoidLevel { get; set; }

        [Column]
        public short? Progress { get; set; }

        public StarSystemThargoidLevelProgress(int id, DateTimeOffset updated, short? progress)
        {
            Id = id;
            Updated = updated;
            Progress = progress;
        }
    }
}
