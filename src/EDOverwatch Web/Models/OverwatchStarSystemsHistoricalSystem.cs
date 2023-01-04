using EDUtils;

namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemsHistoricalSystem : OverwatchStarSystemBase
    {
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public OverwatchThargoidLevel PreviousThargoidLevel { get; }
        public string State { get; }

        public OverwatchStarSystemsHistoricalSystem(StarSystem starSystem, ThargoidCycle thargoidCycle, ThargoidCycle previousThargoidCycle) : base(starSystem)
        {
            if (starSystem.ThargoidLevelHistory == null || !starSystem.ThargoidLevelHistory.Any())
            {
                throw new Exception("starSystem.ThargoidLevelHistory cannot be null");
            }

            StarSystemThargoidLevel thargoidLevel = starSystem.ThargoidLevelHistory
                .OrderByDescending(t => t.CycleStart!.Start)
                .First(t => (t.CycleEnd?.End ?? WeeklyTick.GetLastTick()) <= thargoidCycle.End);

            StarSystemThargoidLevel? previousThargoidLevel = starSystem.ThargoidLevelHistory
                .Where(t => t != thargoidLevel)
                .OrderBy(t => t.CycleStart!.Start)
                .FirstOrDefault(t => t.CycleStart!.Start >= previousThargoidCycle.Start || t.CycleEnd?.End == previousThargoidCycle.End);

            ThargoidLevel = new(thargoidLevel.State);
            PreviousThargoidLevel = new(previousThargoidLevel?.State ?? thargoidLevel.State);

            OverwatchStarSystemsHistoricalSystemState state;
            if (thargoidLevel.State != previousThargoidLevel?.State)
            {
                state = thargoidLevel.State switch
                {
                    StarSystemThargoidLevelState.None => OverwatchStarSystemsHistoricalSystemState.ClearNew,
                    StarSystemThargoidLevelState.Alert => OverwatchStarSystemsHistoricalSystemState.AlertNew,
                    StarSystemThargoidLevelState.Invasion => OverwatchStarSystemsHistoricalSystemState.InvasionNew,
                    StarSystemThargoidLevelState.Controlled => OverwatchStarSystemsHistoricalSystemState.ControlledNew,
                    StarSystemThargoidLevelState.Maelstrom => OverwatchStarSystemsHistoricalSystemState.Maelstrom,
                    StarSystemThargoidLevelState.Recovery => OverwatchStarSystemsHistoricalSystemState.RecoveryNew,
                    _ => OverwatchStarSystemsHistoricalSystemState.Clear,
                };
            }
            else
            {
                state = thargoidLevel.State switch
                {
                    StarSystemThargoidLevelState.None => OverwatchStarSystemsHistoricalSystemState.Clear,
                    StarSystemThargoidLevelState.Alert => OverwatchStarSystemsHistoricalSystemState.Alert,
                    StarSystemThargoidLevelState.Invasion => OverwatchStarSystemsHistoricalSystemState.Invasion,
                    StarSystemThargoidLevelState.Controlled => OverwatchStarSystemsHistoricalSystemState.Controlled,
                    StarSystemThargoidLevelState.Maelstrom => OverwatchStarSystemsHistoricalSystemState.Maelstrom,
                    StarSystemThargoidLevelState.Recovery => OverwatchStarSystemsHistoricalSystemState.Recovery,
                    _ => OverwatchStarSystemsHistoricalSystemState.Clear,
                };
            }
            State = state.GetEnumMemberValue();
        }
    }

    public enum OverwatchStarSystemsHistoricalSystemState
    {
        Clear,
        ClearNew,
        Alert,
        AlertNew,
        Invasion,
        InvasionNew,
        Controlled,
        ControlledNew,
        Recovery,
        RecoveryNew,
        Maelstrom,
    }
}
