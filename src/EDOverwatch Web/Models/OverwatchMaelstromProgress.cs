namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromProgress : OverwatchMaelstrom
    {
        public short HeartsRemaining { get; }
        public decimal HeartProgress { get; }
        public decimal TotalProgress { get; }
        public string State { get; }
        public DateTimeOffset? MeltdownTimeEstimate { get; }
        public DateTimeOffset? CompletionTimeEstimate { get; }

        public OverwatchMaelstromProgress(ThargoidMaelstrom thargoidMaelstrom) :
            base(thargoidMaelstrom)
        {
            HeartsRemaining = thargoidMaelstrom.HeartsRemaining;
            HeartProgress = thargoidMaelstrom.StarSystem?.ThargoidLevel?.CurrentProgress?.ProgressPercent ?? 0m;
            if (HeartsRemaining > 0)
            {
                State = "Active";
                TotalProgress = 1m - (1m / 8m * thargoidMaelstrom.HeartsRemaining - 1m / 8m * HeartProgress);
                CompletionTimeEstimate = thargoidMaelstrom.CompletionTimeEstimate;
            }
            else
            {
                MeltdownTimeEstimate = thargoidMaelstrom.MeltdownTimeEstimate;
                HeartProgress = 1m;
                TotalProgress = 1m;
                if (thargoidMaelstrom.MeltdownTimeEstimate is DateTimeOffset meltdownEstimate && meltdownEstimate > DateTimeOffset.UtcNow)
                {
                    State = "Meltdown";
                }
                else
                {
                    State = "Destroyed";
                }
            }
        }
    }
}
