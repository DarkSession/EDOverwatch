﻿namespace EDOverwatch_Web.Models
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
                StarSystemThargoidLevelState nextSystemState = currentSystemState switch
                {
                    StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                    StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Controlled when starSystem.OriginalPopulation > 0 => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Recovery => StarSystemThargoidLevelState.None,
                    _ => StarSystemThargoidLevelState.None,
                };
                NextSystemState = new(nextSystemState);
                SystemStateChanges = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);
            }
            ProgressLastChange = starSystem.ThargoidLevel?.CurrentProgress?.Updated;
            ProgressLastChecked = starSystem.ThargoidLevel?.CurrentProgress?.LastChecked;
        }
    }
}
