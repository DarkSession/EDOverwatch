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

        [Column("Progress")]
        public short? ProgressOld { get; set; }

        [Column(TypeName = "decimal(10,6)")]
        public decimal? ProgressPercent { get; set; }

        [NotMapped]
        public decimal ProgressReadable => Math.Round((ProgressPercent ?? 0m) * 100, 2);

        [NotMapped]
        public bool IsCompleted => ProgressPercent >= 1m;

        public StarSystemThargoidLevelProgress(int id, DateTimeOffset updated, DateTimeOffset lastChecked, short? progressOld, decimal? progressPercent)
        {
            Id = id;
            Updated = updated;
            LastChecked = lastChecked;
            ProgressOld = progressOld;
            ProgressPercent = progressPercent;
        }
    }
}
