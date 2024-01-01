namespace EDDatabase
{
    [Table("StarSystemThargoidLevelProgress")]
    public class StarSystemThargoidLevelProgress
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public DateTimeOffset Updated { get; set; }

        [Column]
        public DateTimeOffset LastChecked { get; set; }

        public StarSystemThargoidLevel? ThargoidLevel { get; set; }

        [Column]
        public short? Progress { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal? ProgressPercent { get; set; }

        public StarSystemThargoidLevelProgress(int id, DateTimeOffset updated, DateTimeOffset lastChecked, short? progress, decimal? progressPercent)
        {
            Id = id;
            Updated = updated;
            LastChecked = lastChecked;
            Progress = progress;
            ProgressPercent = progressPercent;
        }
    }
}
