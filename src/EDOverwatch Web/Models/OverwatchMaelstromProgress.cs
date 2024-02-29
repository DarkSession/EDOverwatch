namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromProgress : OverwatchMaelstrom
    {
        public short HeartsRemaining { get; }
        public decimal HeartProgress { get; }
        public decimal TotalProgress { get; }

        public OverwatchMaelstromProgress(ThargoidMaelstrom thargoidMaelstrom) :
            base(thargoidMaelstrom)
        {
            HeartsRemaining = thargoidMaelstrom.HeartsRemaining;
            HeartProgress = thargoidMaelstrom.StarSystem?.ThargoidLevel?.CurrentProgress?.ProgressPercent ?? 0m;
            TotalProgress = 1m - (1m / 8m * thargoidMaelstrom.HeartsRemaining - 1m / 8m * HeartProgress);
        }
    }
}
