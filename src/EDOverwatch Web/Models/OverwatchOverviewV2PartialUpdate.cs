using EDOverwatch_Web.Services;
using LazyCache;

namespace EDOverwatch_Web.Models
{
    public class OverwatchOverviewV2PartialUpdate
    {
        public OverwatchOverviewV2Cycle PreviousCycle { get; }
        public OverwatchOverviewV2CycleChange PreviousCycleChanges { get; }
        public OverwatchOverviewV2Cycle CurrentCycle { get; }
        public OverwatchOverviewV2CycleChange NextCycleChanges { get; }
        public OverwatchOverviewV2Cycle NextCyclePrediction { get; }
        public OverwatchStarSystem SystemChanged { get; }
        public DateTimeOffset NextTick => WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);
        public OverviewDataStatus Status { get; }

        protected OverwatchOverviewV2PartialUpdate(
            OverwatchOverviewV2Cycle previousCycle,
            OverwatchOverviewV2CycleChange previousCycleChanges,
            OverwatchOverviewV2Cycle currentCycle,
            OverwatchOverviewV2CycleChange nextCycleChanges,
            OverwatchOverviewV2Cycle nextCyclePrediction,
            OverwatchStarSystem systemChanged,
            OverviewDataStatus status)
        {
            PreviousCycle = previousCycle;
            PreviousCycleChanges = previousCycleChanges;
            CurrentCycle = currentCycle;
            NextCycleChanges = nextCycleChanges;
            NextCyclePrediction = nextCyclePrediction;
            SystemChanged = systemChanged;
            Status = status;
        }

        public static Task<OverwatchOverviewV2PartialUpdate?> Create(long systemAddress, EdDbContext dbContext, IAppCache appCache, EdMaintenance edMaintenance, CancellationToken cancellationToken)
        {
            return CreateInternal(systemAddress, dbContext, edMaintenance, cancellationToken);
        }

        private static async Task<OverwatchOverviewV2PartialUpdate?> CreateInternal(long systemAddress, EdDbContext dbContext, EdMaintenance edMaintenance, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            OverwatchOverviewV2Cycle overviewPreviousCycle;
            OverwatchOverviewV2CycleChange overviewPreviousCycleChanges;
            int previousCycleTitansDefeated;
            {
                ThargoidCycle previousCycle = await dbContext.GetThargoidCycle(now, cancellationToken, -1);
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
                    .Where(s => (s.CycleEnd == previousCycle && s.CycleStart!.Start <= s.CycleEnd!.Start && (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled)) || (s.State == StarSystemThargoidLevelState.Titan && s.Maelstrom!.DefeatCycle == previousCycle && s.CycleEndId == null))
                    .ToListAsync(cancellationToken);

                int alertsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Alert && p.CurrentProgress!.IsCompleted);
                int invasionsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Invasion && p.CurrentProgress!.IsCompleted);
                int controlsDefended = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Controlled && p.CurrentProgress!.IsCompleted);
                previousCycleTitansDefeated = previousCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Titan && p.Maelstrom?.DefeatCycleId == previousCycle.Id);
                int thargoidInvasionStarted = previousCycleStates.Count(p => !(p.CurrentProgress?.IsCompleted ?? false) && (p.State == StarSystemThargoidLevelState.Alert && (p.StarSystem?.OriginalPopulation ?? 0) > 0));
                int thargoidsGained = previousCycleStates.Count(p => (p.CurrentProgress == null || !p.CurrentProgress!.IsCompleted) && (p.State == StarSystemThargoidLevelState.Invasion || (p.State == StarSystemThargoidLevelState.Alert && p.StarSystem?.OriginalPopulation == 0)));

                overviewPreviousCycleChanges = new(alertsDefended, invasionsDefended, controlsDefended, previousCycleTitansDefeated, thargoidInvasionStarted, thargoidsGained);
            }

            ThargoidCycle nextCycle = await dbContext.GetThargoidCycle(now, cancellationToken, 1);
            OverwatchOverviewV2Cycle overviewCurrentCycle;
            OverwatchOverviewV2CycleChange overviewNextCycleChanges;
            OverwatchOverviewV2Cycle overviewNextCyclePrediction;
            {

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
                    .Where(s => (s.CycleEnd == null || (s.CycleEnd == currentThargoidCycle && s.CycleStart!.Start <= s.CycleEnd!.Start)) || (s.State == StarSystemThargoidLevelState.Titan && s.Maelstrom!.DefeatCycle == currentThargoidCycle && s.CycleEndId == null))
                    .ToListAsync(cancellationToken);

                int alertsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Alert && (p.CurrentProgress?.IsCompleted ?? false));
                int invasionsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Invasion && (p.CurrentProgress?.IsCompleted ?? false));
                int controlsDefended = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Controlled && (p.CurrentProgress?.IsCompleted ?? false));
                int titansDefeated = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Titan && p.Maelstrom?.DefeatCycleId == currentThargoidCycle.Id);
                int thargoidInvasionStart = currentCycleStates.Count(p => !(p.CurrentProgress?.IsCompleted ?? false) && p.State == StarSystemThargoidLevelState.Alert && (p.StarSystem?.OriginalPopulation ?? 0) > 0);
                int thargoidInvasionSuccess = currentCycleStates.Count(p => !(p.CurrentProgress?.IsCompleted ?? false) && p.State == StarSystemThargoidLevelState.Invasion && p.StateExpires?.Id == currentThargoidCycle.Id);
                int thargoidGainFromAlert = currentCycleStates.Count(p => !(p.CurrentProgress?.IsCompleted ?? false) && p.State == StarSystemThargoidLevelState.Alert && p.StarSystem?.OriginalPopulation == 0);
                int thargoidsGain = thargoidGainFromAlert + thargoidInvasionSuccess;

                overviewNextCycleChanges = new(alertsDefended, invasionsDefended, controlsDefended, titansDefeated, thargoidInvasionStart, thargoidsGain);

                int alertsPredicted = await dbContext.AlertPredictions.CountAsync(a => a.Cycle == nextCycle && a.AlertLikely && a.Type == AlertPredictionType.Alert, cancellationToken);
                int invasionsPredicted = invasionCount - thargoidInvasionSuccess - invasionsDefended + thargoidInvasionStart + await dbContext.AlertPredictions.CountAsync(a => a.Cycle == nextCycle && a.AlertLikely && a.Type == AlertPredictionType.Invasion, cancellationToken);
                int controlledPredicted = controlledCount - controlsDefended + thargoidsGain;
                int titanPredicted = titanCount - previousCycleTitansDefeated;
                int recoveryCompleted = currentCycleStates.Count(p => p.State == StarSystemThargoidLevelState.Recovery && (p.CurrentProgress?.IsCompleted ?? false));
                int newRecoveries = currentCycleStates.Count(p => (p.CurrentProgress?.IsCompleted ?? false) && (p.State == StarSystemThargoidLevelState.Invasion || p.State == StarSystemThargoidLevelState.Controlled) && p.StarSystem?.OriginalPopulation > 0);
                int recoveryPredicted = recoveryCount - recoveryCompleted + newRecoveries;
                overviewNextCyclePrediction = new(nextCycle.Start, nextCycle.End, alertsPredicted, invasionsPredicted, controlledPredicted, titanPredicted, recoveryPredicted);
            }

            var lastTick = WeeklyTick.GetLastTick();
            var stationMaxAge = DateTimeOffset.UtcNow.AddDays(-1);
            var signalMaxAge = lastTick.AddDays(-7);
            if (lastTick < stationMaxAge)
            {
                stationMaxAge = lastTick;
            }

            (var startDateHour, var totalActivity) = await OverwatchStarSystemFull.GetTotalPlayerActivity(dbContext);

            var system = await dbContext.StarSystems
               .AsNoTracking()
               .Where(s => s.SystemAddress == systemAddress)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .Include(s => s.ThargoidLevel!.CurrentProgress)
                .Include(s => s.ThargoidLevel!.ProgressHistory!.Where(p => p.IsCompleted))
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
                    StationsUnderRepair = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderRepairs && StationType.WarRelevantAssetTypes.Contains(s.Type!.Name)).Count(),
                    OdysseySettlements = s.Stations!.Any(s => s.Type!.Name == StationType.OdysseySettlementType),
                    FederalFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Federation),
                    EmpireFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Empire),
                    AXConflictZones = s.FssSignals!.Any(f => f.Type == StarSystemFssSignalType.AXCZ && f.LastSeen >= signalMaxAge),
                    GroundPortUnderAttack = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderAttack && StationType.WarGroundAssetTypes.Contains(s.Type!.Name)).Any(),
                    HasAlertPredicted = dbContext.AlertPredictions.Any(a => a.StarSystem == s && a.Cycle == nextCycle && a.AlertLikely),
                    PlayerActivityCount = s.PlayerActivities!.Count(p => p.DateHour >= startDateHour),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (system is null)
            {
                return null;
            }

            var starSystem = system.StarSystem;
            var effortFocus = 0m;
            if (totalActivity != 0)
            {
                effortFocus = Math.Round((decimal)system.PlayerActivityCount / (decimal)totalActivity, 4);
            }

            var specialFactionOperations = system.SpecialFactionOperations
                .Select(s => new OverwatchStarSystemSpecialFactionOperation(s.Short, s.Name))
                .ToList();

            var overwatchStarSystem = new OverwatchStarSystemFull(
                    starSystem,
                    effortFocus,
                    factionAxOperations: system.FactionAxOperations,
                    factionGeneralOperations: system.FactionGeneralOperations,
                    factionRescueOperations: system.FactionRescueOperations,
                    factionLogisticsOperations: system.FactionLogisticsOperations,
                    specialFactionOperations,
                    system.StationsUnderRepair,
                    system.StationsDamaged,
                    system.StationsUnderAttack,
                    system.OdysseySettlements,
                    system.FederalFaction,
                    system.EmpireFaction,
                    system.AXConflictZones,
                    system.GroundPortUnderAttack,
                    system.HasAlertPredicted);

            OverviewDataStatus status = OverviewDataStatus.Default;
            if (now.DayOfWeek == DayOfWeek.Thursday)
            {
                if (edMaintenance.IsInMaintenanceMode || (now.Hour == 7 && now.Minute < 30))
                {
                    status = OverviewDataStatus.TickInProgress;
                }
                else if (now.Hour >= 7 && now.Hour < 11)
                {
                    status = OverviewDataStatus.UpdatePending;
                }
            }

            return new OverwatchOverviewV2PartialUpdate(overviewPreviousCycle, overviewPreviousCycleChanges, overviewCurrentCycle, overviewNextCycleChanges, overviewNextCyclePrediction, overwatchStarSystem, status);
        }
    }
}
