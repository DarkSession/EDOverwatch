namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemStateProgress
    {
        public decimal? ProgressPercent { get; }
        public bool IsCompleted { get; }
        public OverwatchThargoidLevel? NextSystemState { get; }
        public DateTimeOffset? SystemStateChanges { get; }
        public DateTimeOffset? ProgressLastChange { get; }
        public DateTimeOffset? ProgressLastChecked { get; }
        public OverwatchStarSystemStateProgress(StarSystem starSystem, decimal? progressPercent, StarSystemThargoidLevelState currentSystemState)
        {
            ProgressPercent = progressPercent;
            IsCompleted = ProgressPercent >= 1;
            if (IsCompleted)
            {
                StarSystemThargoidLevelState nextSystemState = StarSystemThargoidLevel.GetNextThargoidState(currentSystemState, starSystem.OriginalPopulation > 0, true);
                NextSystemState = new(nextSystemState);
                SystemStateChanges = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);
            }
            ProgressLastChange = starSystem.ThargoidLevel?.CurrentProgress?.Updated;
            ProgressLastChecked = starSystem.ThargoidLevel?.CurrentProgress?.LastChecked;
        }
    }
}
