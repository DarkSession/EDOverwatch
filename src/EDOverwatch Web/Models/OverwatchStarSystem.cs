namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystem
    {
        public long SystemAddress { get; set; }
        public string Name { get; }
        public OverwatchStarSystemCoordinates Coordinates { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal? ProgressPercent { get; }
        public decimal EffortFocus { get; }
        public int FactionOperations { get; protected set; }
        public List<OverwatchStarSystemSpecialFactionOperation> SpecialFactionOperations { get; }
        public int StationsUnderRepair { get; protected set; }
        public int StationsDamaged { get; protected set; }
        public int StationsUnderAttack { get; protected set; }
        public DateTimeOffset StateStartCycle { get; }
        public OverwatchStarSystemStateExpires? StateExpiration { get; }
        public OverwatchStarSystemStateProgress StateProgress { get; }
        public long Population { get; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus, int factionOperations, List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations, int stationsUnderRepair, int stationsDamaged, int stationsUnderAttack)
        {
            SystemAddress = starSystem.SystemAddress;
            Name = starSystem.Name;
            Coordinates = new(starSystem.LocationX, starSystem.LocationY, starSystem.LocationZ);
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            ProgressPercent = (Progress != null) ? (decimal)Progress / 100m : null;
            EffortFocus = effortFocus;
            FactionOperations = factionOperations;
            SpecialFactionOperations = specialFactionOperations;
            StationsUnderRepair = stationsUnderRepair;
            StationsDamaged = stationsDamaged;
            StationsUnderAttack = stationsUnderAttack;
            StateStartCycle = starSystem.ThargoidLevel?.CycleStart?.Start ?? throw new Exception("Thargoid level must have a cycle property");
            Population = starSystem.Population;
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
            StateProgress = new(ProgressPercent, starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
        }
    }

    public class OverwatchStarSystemSpecialFactionOperation
    {
        public string Tag { get; }
        public string Name { get; }
        public OverwatchStarSystemSpecialFactionOperation(string tag, string name)
        {
            Tag = tag;
            Name = name;
        }
    }

    public class OverwatchStarSystemCoordinates
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }
        public OverwatchStarSystemCoordinates(decimal x, decimal y, decimal z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class OverwatchStarSystemStateExpires
    {
        public DateTimeOffset StateExpires { get; }
        public DateTimeOffset CurrentCycleEnds { get; }
        public short? RemainingCycles { get; }

        public OverwatchStarSystemStateExpires(DateTimeOffset stateExpires, DateTimeOffset currentCycleEnds, short? remainingCycles)
        {
            StateExpires = stateExpires;
            CurrentCycleEnds = currentCycleEnds;
            RemainingCycles = remainingCycles;
        }
    }

    public class OverwatchStarSystemStateProgress
    {
        public decimal? ProgressPercent { get; }
        public bool IsCompleted { get; }
        public OverwatchThargoidLevel? NextSystemState { get; }
        public DateTimeOffset? SystemStateChanges { get; }
        public OverwatchStarSystemStateProgress(decimal? progressPercent, StarSystemThargoidLevelState currentSystemState)
        {
            ProgressPercent = progressPercent;
            IsCompleted = (ProgressPercent >= 1);
            if (IsCompleted)
            {
                StarSystemThargoidLevelState nextSystemState = currentSystemState switch
                {
                    StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                    StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Controlled => StarSystemThargoidLevelState.Recapture,
                    StarSystemThargoidLevelState.Recapture => StarSystemThargoidLevelState.Recovery,
                    StarSystemThargoidLevelState.Recovery => StarSystemThargoidLevelState.None,
                    _ => StarSystemThargoidLevelState.None,
                };
                NextSystemState = new(nextSystemState);
                SystemStateChanges = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);
            }
        }
    }
}
