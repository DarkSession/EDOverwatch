namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem : OverwatchStarSystemBase
    {
        public long PopulationOriginal { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal? ProgressPercent { get; }
        public DateTimeOffset StateStartCycle { get; }
        public OverwatchStarSystemStateExpires? StateExpiration { get; }
        public OverwatchStarSystemStateProgress StateProgress { get; }
        public double DistanceToMaelstrom { get; }
        [Obsolete]
        public bool BarnacleMatrixInSystem { get; }
        public bool ThargoidSpireSiteInSystem { get; }

        public OverwatchStarSystem(StarSystem starSystem)
            : base(starSystem)
        {
            PopulationOriginal = starSystem.OriginalPopulation;
            ThargoidLevel = new(starSystem.ThargoidLevel);
            Progress = starSystem.ThargoidLevel?.Progress;
            ProgressPercent = (Progress != null) ? (decimal)Progress / 100m : null;
            if (starSystem.ThargoidLevel?.Maelstrom?.StarSystem != null)
            {
                DistanceToMaelstrom = Math.Round(starSystem.DistanceTo(starSystem.ThargoidLevel.Maelstrom.StarSystem), 4);
            }
#pragma warning disable CS0612 // Type or member is obsolete
            BarnacleMatrixInSystem = starSystem.BarnacleMatrixInSystem;
#pragma warning restore CS0612 // Type or member is obsolete
            ThargoidSpireSiteInSystem = starSystem.BarnacleMatrixInSystem;
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
            StateProgress = new(starSystem, ProgressPercent, starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
        }
    }
}
