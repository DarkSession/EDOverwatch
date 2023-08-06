namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewV2
    {
        public OverwatchOverviewV2Cycle PreviousCycle { get; }
        public OverwatchOverviewV2CycleChange PreviousCycleChanges { get; }
        public OverwatchOverviewV2Cycle CurrentCycle { get; }
        public OverwatchOverviewV2CycleChange NextCycleChanges { get; }
        public OverwatchOverviewV2Cycle NextCyclePrediction { get; }
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>()
            .Where(s => s > StarSystemThargoidLevelState.None)
            .Select(s => new OverwatchThargoidLevel(s))
            .ToList();
        public List<OverwatchStarSystem> Systems { get; }
        public DateTimeOffset NextTick => WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);

        public OverwatchOverviewV2(OverwatchOverviewV2Cycle previousCycle, OverwatchOverviewV2CycleChange previousCycleChanges, OverwatchOverviewV2Cycle currentCycle, OverwatchOverviewV2CycleChange nextCycleChanges, OverwatchOverviewV2Cycle nextCyclePrediction, List<ThargoidMaelstrom> thargoidMaelstroms, List<OverwatchStarSystem> systems)
        {
            PreviousCycle = previousCycle;
            PreviousCycleChanges = previousCycleChanges;
            CurrentCycle = currentCycle;
            NextCycleChanges = nextCycleChanges;
            NextCyclePrediction = nextCyclePrediction;
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
            Systems = systems;
        }

        public static async Task<OverwatchOverviewV2> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

            OverwatchOverviewV2Cycle overviewPreviousCycle;
            OverwatchOverviewV2CycleChange overviewPreviousCycleChanges;
            {
                ThargoidCycle previousCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, -1);
                var previousCycleSum = await dbContext.ThargoidMaelstromHistoricalSummaries
                    .AsNoTracking()
                    .Where(t => t.Cycle == previousCycle)
                    .GroupBy(t => t.State)
                    .Select(t => new
                    {
                        State = t.Key,
                        Count = t.Sum(x => x.Amount),
                    })
                    .ToListAsync(cancellationToken);

                int alertCount = previousCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Alert)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int invasionCount = previousCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Invasion)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int controlledCount = previousCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Controlled)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int titanCount = previousCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Titan)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int recoveryCount = previousCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Recovery)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                overviewPreviousCycle = new(previousCycle.Start, previousCycle.End, alertCount, invasionCount, controlledCount, titanCount, recoveryCount);

                List<StarSystemThargoidLevel> previousCycleStates = await dbContext.StarSystemThargoidLevels
                    .AsNoTracking()
                    .Include(s => s.StarSystem)
                    .Where(s => s.CycleEnd == previousCycle && s.CycleStart!.Start <= s.CycleEnd!.Start && (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled))
                    .ToListAsync(cancellationToken);

                int alertsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Alert && p.Progress >= 100);
                int invasionsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Invasion && p.Progress >= 100);
                int controlsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Controlled && p.Progress >= 100);
                int thargoidsGained = previousCycleStates.Count(p => (p.Progress == null || p.Progress < 100) && (p.State == StarSystemThargoidLevelState.Invasion || (p.State == StarSystemThargoidLevelState.Alert && p.StarSystem?.OriginalPopulation == 0)));

                overviewPreviousCycleChanges = new(alertsDefended, invasionsDefended, controlsDefended, thargoidsGained);
            }

            OverwatchOverviewV2Cycle overviewCurrentCycle;
            OverwatchOverviewV2CycleChange overviewNextCycleChanges;
            OverwatchOverviewV2Cycle overviewNextCyclePrediction;
            {
                ThargoidCycle nextCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

                var currentCycleSum = await dbContext.ThargoidMaelstromHistoricalSummaries
                    .AsNoTracking()
                    .Where(t => t.Cycle == currentThargoidCycle)
                    .GroupBy(t => t.State)
                    .Select(t => new
                    {
                        State = t.Key,
                        Count = t.Sum(x => x.Amount),
                    })
                    .ToListAsync(cancellationToken);

                int alertCount = currentCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Alert)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int invasionCount = currentCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Invasion)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int controlledCount = currentCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Controlled)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int titanCount = currentCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Titan)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                int recoveryCount = currentCycleSum
                    .Where(p => p.State == StarSystemThargoidLevelState.Recovery)
                    .Select(p => p.Count)
                    .DefaultIfEmpty()
                    .Sum();
                overviewCurrentCycle = new(currentThargoidCycle.Start, currentThargoidCycle.End, alertCount, invasionCount, controlledCount, titanCount, recoveryCount);

                List<StarSystemThargoidLevel> currentCycleStates = await dbContext.StarSystemThargoidLevels
                    .AsNoTracking()
                    .Include(s => s.StarSystem)
                    .Where(s => (s.CycleEnd == null || (s.CycleEnd == currentThargoidCycle && s.CycleStart!.Start <= s.CycleEnd!.Start)) && s.StateExpires == currentThargoidCycle)
                    .ToListAsync(cancellationToken);

                int alertsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Alert && p.Progress >= 100);
                int invasionsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Invasion && p.Progress >= 100);
                int controlsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Controlled && p.Progress >= 100);
                int thargoidsGain = currentCycleStates.Count(p => (p.Progress == null || p.Progress < 100) && (p.State == StarSystemThargoidLevelState.Invasion || (p.State == StarSystemThargoidLevelState.Alert && p.StarSystem?.OriginalPopulation == 0)));

                overviewNextCycleChanges = new(alertsDefended, invasionsDefended, controlsDefended, thargoidsGain);

                int alertsPredicted = await dbContext.AlertPredictions.CountAsync(a => a.Cycle == nextCycle && a.AlertLikely, cancellationToken);
                int invasionsPredicted = invasionCount - invasionsDefended + currentCycleStates.Count(p => (p.Progress == null || p.Progress < 100) && p.State == StarSystemThargoidLevelState.Alert && p.StarSystem?.OriginalPopulation > 0) - currentCycleStates.Count(p => (p.Progress == null || p.Progress < 100) && p.State == StarSystemThargoidLevelState.Invasion);
                int controlledPredicted = controlledCount - controlsDefended + thargoidsGain;
                int titanPredicted = titanCount;
                int recoveryPredicted = recoveryCount - currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Recovery && p.Progress >= 100) + currentCycleStates.Count(p => p.Progress >= 100 && (p.State == StarSystemThargoidLevelState.Invasion || p.State  == StarSystemThargoidLevelState.Controlled) && p.StarSystem?.OriginalPopulation > 0);
                overviewNextCyclePrediction = new(nextCycle.Start, nextCycle.End, alertsPredicted, invasionsPredicted, controlledPredicted, titanPredicted, recoveryPredicted);
            }

            DateTimeOffset lastTick = WeeklyTick.GetLastTick();
            DateTimeOffset stationMaxAge = DateTimeOffset.UtcNow.AddDays(-1);
            if (lastTick < stationMaxAge)
            {
                stationMaxAge = lastTick;
            }

            var systems = await dbContext.StarSystems
               .AsNoTracking()
               .Where(s =>
                           s.ThargoidLevel != null &&
                           s.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert &&
                           s.ThargoidLevel.Maelstrom != null &&
                           s.ThargoidLevel.Maelstrom.StarSystem != null)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .Include(s => s.ThargoidLevel!.CurrentProgress)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    StarSystem = s,
                    FactionAxOperations = s.FactionOperations!.Where(f => f.Status == DcohFactionOperationStatus.Active && f.Type == DcohFactionOperationType.AXCombat).Count(),
                    FactionGeneralOperations = s.FactionOperations!.Where(f => f.Status == DcohFactionOperationStatus.Active && f.Type == DcohFactionOperationType.General).Count(),
                    FactionRescueOperations = s.FactionOperations!.Where(f => f.Status == DcohFactionOperationStatus.Active && f.Type == DcohFactionOperationType.Rescue).Count(),
                    FactionLogisticsOperations = s.FactionOperations!.Where(f => f.Status == DcohFactionOperationStatus.Active && f.Type == DcohFactionOperationType.Logistics).Count(),
                    SpecialFactionOperations = s.FactionOperations!.Where(f => f.Faction!.SpecialFaction && f.Status == DcohFactionOperationStatus.Active).Select(s => new
                    {
                        s.Faction!.Name,
                        s.Faction!.Short,
                    }),
                    StationsUnderAttack = s.Stations!.Where(s => s.Updated > stationMaxAge && (s.State == StationState.UnderAttack)).Count(),
                    StationsDamaged = s.Stations!.Where(s => s.Updated > stationMaxAge && (s.State == StationState.Damaged)).Count(),
                    StationsUnderRepair = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderRepairs).Count(),
                })
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            Dictionary<WarEffortTypeGroup, long> totalEffortSums = await WarEffort.GetTotalWarEfforts(dbContext, cancellationToken);
            DateOnly startDate = WarEffort.GetWarEffortFocusStartDate();

            var efforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w =>
                        w.Date >= startDate &&
                        w.StarSystem!.WarRelevantSystem &&
                        w.Side == WarEffortSide.Humans)
                .GroupBy(w => new
                {
                    w.StarSystemId,
                    w.Type,
                })
                .Select(w => new
                {
                    w.Key.StarSystemId,
                    w.Key.Type,
                    Amount = w.Sum(g => g.Amount),
                })
                .ToListAsync(cancellationToken);

            List<OverwatchStarSystem> overwatchStarSystems = new();

            foreach (var system in systems)
            {
                StarSystem starSystem = system.StarSystem;
                decimal effortFocus = 0;
                if (totalEffortSums.Any())
                {
                    effortFocus = WarEffort.CalculateSystemFocus(efforts.Where(e => e.StarSystemId == starSystem.Id).Select(e => new WarEffortTypeSum(e.Type, e.Amount)), totalEffortSums);
                }
                List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations = system.SpecialFactionOperations
                    .Select(s => new OverwatchStarSystemSpecialFactionOperation(s.Short, s.Name))
                    .ToList();
                overwatchStarSystems.Add(new OverwatchStarSystem(
                    starSystem,
                    effortFocus,
                    factionAxOperations: system.FactionAxOperations,
                    factionGeneralOperations: system.FactionGeneralOperations,
                    factionRescueOperations: system.FactionRescueOperations,
                    factionLogisticsOperations: system.FactionLogisticsOperations,
                    specialFactionOperations,
                    system.StationsUnderRepair,
                    system.StationsDamaged,
                    system.StationsUnderAttack));
            }

            return new OverwatchOverviewV2(overviewPreviousCycle, overviewPreviousCycleChanges, overviewCurrentCycle, overviewNextCycleChanges, overviewNextCyclePrediction, maelstroms, overwatchStarSystems);
        }
    }

    public class OverwatchOverviewV2Cycle
    {
        public DateTimeOffset CycleStart { get; }
        public DateTimeOffset CycleEnd { get; }
        public int Alerts { get; }
        public int Invasions { get; }
        public int Controls { get; }
        public int Titans { get; }
        public int Recovery { get; }

        public OverwatchOverviewV2Cycle(DateTimeOffset cycleStart, DateTimeOffset cycleEnd, int alerts, int invasions, int controls, int titans, int recovery)
        {
            CycleStart = cycleStart;
            CycleEnd = cycleEnd;
            Alerts = alerts;
            Invasions = invasions;
            Controls = controls;
            Titans = titans;
            Recovery = recovery;
        }
    }

    public class OverwatchOverviewV2CycleChange
    {
        public int AlertsDefended { get; }
        public int InvasionsDefended { get; }
        public int ControlsDefended { get; }
        public int ThargoidsGained { get; }

        public OverwatchOverviewV2CycleChange(int alertsDefended, int invasionsDefended, int controlsDefended, int thargoidsGained)
        {
            AlertsDefended = alertsDefended;
            InvasionsDefended = invasionsDefended;
            ControlsDefended = controlsDefended;
            ThargoidsGained = thargoidsGained;
        }
    }
}
