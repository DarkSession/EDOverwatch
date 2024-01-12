namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemsHistoricalSystem : OverwatchStarSystemBase
    {
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public OverwatchThargoidLevel PreviousThargoidLevel { get; }
        public string State { get; }
        public long PopulationOriginal { get; }
        public double DistanceToMaelstrom { get; }
        [Obsolete("Use ThargoidSpireSiteInSystem")]
        public bool BarnacleMatrixInSystem { get; }
        public bool ThargoidSpireSiteInSystem { get; }
        public int? Progress { get; }
        public decimal? ProgressPercent { get; }
        public decimal? ProgressUncapped { get; }
        public bool ProgressIsCompleted { get; }
        public DateTimeOffset? StateExpires { get; }

        public OverwatchStarSystemsHistoricalSystem(StarSystem starSystem, ThargoidCycle thargoidCycle) :
            base(starSystem)
        {
            if (starSystem.ThargoidLevelHistory == null || !starSystem.ThargoidLevelHistory.Any())
            {
                throw new Exception("starSystem.ThargoidLevelHistory cannot be null");
            }

            PopulationOriginal = starSystem.OriginalPopulation;
#pragma warning disable CS0618 // Type or member is obsolete
            BarnacleMatrixInSystem = starSystem.BarnacleMatrixInSystem;
#pragma warning restore CS0618 // Type or member is obsolete
            ThargoidSpireSiteInSystem = starSystem.BarnacleMatrixInSystem;

            try
            {
                StarSystemThargoidLevel thargoidLevel = starSystem.ThargoidLevelHistory
                    .OrderByDescending(t => t.CycleStart!.Start)
                    .First(t => t.CycleEnd!.End >= thargoidCycle.End);

                StarSystemThargoidLevel? previousThargoidLevel = starSystem.ThargoidLevelHistory
                    .OrderBy(t => t.CycleStart!.Start)
                    .FirstOrDefault(t => t.CycleStart!.Start < thargoidCycle.Start);

                ThargoidLevel = new(thargoidLevel);
                PreviousThargoidLevel = new(previousThargoidLevel ?? thargoidLevel);

                OverwatchStarSystemsHistoricalSystemState state;
                if (previousThargoidLevel != null && thargoidLevel.State != previousThargoidLevel.State)
                {
                    state = thargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.None => OverwatchStarSystemsHistoricalSystemState.ClearNew,
                        StarSystemThargoidLevelState.Alert => OverwatchStarSystemsHistoricalSystemState.AlertNew,
                        StarSystemThargoidLevelState.Invasion => OverwatchStarSystemsHistoricalSystemState.InvasionNew,
                        StarSystemThargoidLevelState.Controlled => OverwatchStarSystemsHistoricalSystemState.ControlledNew,
                        StarSystemThargoidLevelState.Titan => OverwatchStarSystemsHistoricalSystemState.Titan,
                        StarSystemThargoidLevelState.Recovery => OverwatchStarSystemsHistoricalSystemState.RecoveryNew,
                        _ => OverwatchStarSystemsHistoricalSystemState.Clear,
                    };
                }
                else
                {
                    state = thargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.None => OverwatchStarSystemsHistoricalSystemState.Clear,
                        StarSystemThargoidLevelState.Alert => OverwatchStarSystemsHistoricalSystemState.AlertNew,
                        StarSystemThargoidLevelState.Invasion => OverwatchStarSystemsHistoricalSystemState.Invasion,
                        StarSystemThargoidLevelState.Controlled => OverwatchStarSystemsHistoricalSystemState.Controlled,
                        StarSystemThargoidLevelState.Titan => OverwatchStarSystemsHistoricalSystemState.Titan,
                        StarSystemThargoidLevelState.Recovery => OverwatchStarSystemsHistoricalSystemState.Recovery,
                        _ => OverwatchStarSystemsHistoricalSystemState.Clear,
                    };
                }
                State = state.GetEnumMemberValue();

                DistanceToMaelstrom = Math.Round(thargoidLevel.Maelstrom?.StarSystem?.DistanceTo(starSystem) ?? 0, 4);
                if ((thargoidLevel.CurrentProgress?.IsCompleted ?? false) && thargoidLevel.CycleEndId == thargoidCycle.Id)
                {
                    ProgressPercent = thargoidLevel.CurrentProgress.ProgressPercent ?? 0m;
                    Progress = (int)Math.Floor((decimal)ProgressPercent * 100m);
                }
                else if (thargoidLevel.ProgressHistory?.FirstOrDefault() is StarSystemThargoidLevelProgress progress && progress.Updated != default)
                {
                    ProgressPercent = progress.ProgressPercent ?? 0m;
                    Progress = (int)Math.Floor((decimal)ProgressPercent * 100m);
                }
                if (Progress > 100)
                {
                    Progress = 100;
                }
                ProgressIsCompleted = Progress is int p && p >= 100;
                StateExpires = thargoidLevel.StateExpires?.End;
            }
            catch (Exception e)
            {
                throw new Exception($"OverwatchStarSystemsHistoricalSystem could not be created for system {starSystem.Name}", e);
            }
        }
    }

    public enum OverwatchStarSystemsHistoricalSystemState
    {
        Clear,
        ClearNew,
        AlertNew,
        Invasion,
        InvasionNew,
        Controlled,
        ControlledNew,
        Recovery,
        RecoveryNew,
        Titan,
    }
}
