namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem : OverwatchStarSystemBase
    {
        public long PopulationOriginal { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        [Obsolete("Use StateProgress object instead")]
        public short? Progress { get; }
        [Obsolete("Use StateProgress object instead")]
        public decimal? ProgressPercent { get; }
        public DateTimeOffset StateStartCycle { get; }
        public OverwatchStarSystemStateExpires? StateExpiration { get; }
        public OverwatchStarSystemStateProgress StateProgress { get; }
        public double DistanceToMaelstrom { get; }
        [Obsolete("Use ThargoidSpireSiteInSystem instead")]
        public bool BarnacleMatrixInSystem { get; }
        public bool ThargoidSpireSiteInSystem { get; }
        public string? ThargoidSpireSiteBody { get; }

        public OverwatchStarSystem(StarSystem starSystem, bool hasAlertPrediction)
            : base(starSystem)
        {
            PopulationOriginal = starSystem.OriginalPopulation;
            ThargoidLevel = new(starSystem.ThargoidLevel);
            if (starSystem.ThargoidLevel?.Maelstrom?.StarSystem != null)
            {
                DistanceToMaelstrom = Math.Round(starSystem.DistanceTo(starSystem.ThargoidLevel.Maelstrom.StarSystem), 4);
            }
            StateProgress = new(starSystem, starSystem.ThargoidLevel?.CurrentProgress?.ProgressPercent, starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None, hasAlertPrediction);
#pragma warning disable CS0618 // Type or member is obsolete
            ProgressPercent = StateProgress.ProgressPercent;
            Progress = starSystem.ThargoidLevel?.CurrentProgress?.ProgressLegacy;
            BarnacleMatrixInSystem = starSystem.BarnacleMatrixInSystem;
#pragma warning restore CS0618 // Type or member is obsolete
            ThargoidSpireSiteInSystem = starSystem.BarnacleMatrixInSystem;
            ThargoidSpireSiteBody = starSystem.SpireSiteBody;
            StateStartCycle = starSystem.ThargoidLevel?.CycleStart?.Start ?? throw new Exception("Thargoid level must have a cycle property");
            if (starSystem.ThargoidLevel!.StateExpires != null)
            {
                DateTimeOffset stateExpires = starSystem.ThargoidLevel.StateExpires.End;
                DateTimeOffset currentCycleEnds;
                short? cyclesLeft = 0;
                if (starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Recovery)
                {
                    currentCycleEnds = stateExpires;
                    cyclesLeft = null;
                }
                else
                {
                    currentCycleEnds = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);
                    if ((stateExpires - currentCycleEnds).TotalDays >= 7)
                    {
                        cyclesLeft = (short?)Math.Floor((double)(stateExpires - currentCycleEnds).TotalDays / 7d);
                    }
                }
                StateExpiration = new(stateExpires, currentCycleEnds, cyclesLeft);
            }
        }
    }
}
