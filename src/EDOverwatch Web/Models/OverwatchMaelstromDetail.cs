﻿namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetail : OverwatchMaelstromProgress
    {
        public List<OverwatchStarSystem> Systems { get; }
        public OverwatchAlertPredictionMaelstrom AlertPrediction { get; }
        public List<OverwatchOverviewMaelstromHistoricalSummary> MaelstromHistory { get; set; } = [];
        public List<OverwatchThargoidCycle> ThargoidCycles { get; }
        public TitanDamageResistance DamageResistance { get; }

        protected OverwatchMaelstromDetail(
            ThargoidMaelstrom thargoidMaelstrom,
            List<OverwatchStarSystem> systems,
            List<AlertPrediction> alertPredictions,
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory,
            List<OverwatchThargoidCycle> thargoidCycles) :
            base(thargoidMaelstrom)
        {
            Systems = systems;
            MaelstromHistory = maelstromHistory;
            ThargoidCycles = thargoidCycles;
            AlertPrediction = new(thargoidMaelstrom, alertPredictions);
            DamageResistance = TitanDamageResistance.GetDamageResistance(systems.Count(s => s.ThargoidLevel.Level == StarSystemThargoidLevelState.Controlled), thargoidMaelstrom.HeartsRemaining);
        }

        public static async Task<OverwatchMaelstromDetail> Create(int maelstromId, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidMaelstrom? maelstrom = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(s => s.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .FirstOrDefaultAsync(s => s.Id == maelstromId, cancellationToken) ?? throw new Exception("Maelstrom not found.");
            if (maelstrom?.StarSystem == null)
            {
                throw new Exception("StarSystem is null");
            }

            ThargoidCycle nextThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

            DateTimeOffset lastTick = WeeklyTick.GetLastTick();
            DateTimeOffset stationMaxAge = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset signalMaxAge = lastTick.AddDays(-7);
            if (lastTick < stationMaxAge)
            {
                stationMaxAge = lastTick;
            }

            (var startDateHour, var totalActivity) = await OverwatchStarSystemFull.GetTotalPlayerActivity(dbContext);

            var systems = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevel!.Maelstrom == maelstrom && s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .Include(s => s.ThargoidLevel!.CurrentProgress)
                .Include(s => s.ThargoidLevel!.ProgressHistory!.Where(p => p.IsCompleted))
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
                    StationsUnderAttack = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderAttack).Count(),
                    StationsDamaged = s.Stations!.Where(s => s.Updated > stationMaxAge && (s.State == StationState.Damaged)).Count(),
                    StationsUnderRepair = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderRepairs && StationType.WarRelevantAssetTypes.Contains(s.Type!.Name)).Count(),
                    OdysseySettlements = s.Stations!.Any(s => s.Type!.Name == StationType.OdysseySettlementType),
                    FederalFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Federation),
                    EmpireFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Empire),
                    AXConflictZones = s.FssSignals!.Any(f => f.Type == StarSystemFssSignalType.AXCZ && f.LastSeen >= signalMaxAge),
                    GroundPortUnderAttack = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderAttack && StationType.WarGroundAssetTypes.Contains(s.Type!.Name)).Any(),
                    HasAlertPredicted = dbContext.AlertPredictions.Any(a => a.StarSystem == s && a.Cycle == nextThargoidCycle && a.AlertLikely),
                    PlayerActivityCount = s.PlayerActivities!.Count(p => p.DateHour >= startDateHour),
                })
                .ToListAsync(cancellationToken);

            List<OverwatchStarSystem> resultStarSystems = [];
            foreach (var system in systems)
            {
                var starSystem = system.StarSystem;
                var effortFocus = 0m;
                if (totalActivity != 0)
                {
                    effortFocus = Math.Round((decimal)system.PlayerActivityCount / (decimal)totalActivity, 4);
                }

                var specialFactionOperations = system.SpecialFactionOperations
                    .Select(s => new OverwatchStarSystemSpecialFactionOperation(s.Short, s.Name))
                    .ToList();
                resultStarSystems.Add(new OverwatchStarSystemFull(
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
                    system.HasAlertPredicted));
            }

            List<AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Attackers!)
                .ThenInclude(a => a.StarSystem!.ThargoidLevel)
                .Where(a => a.Maelstrom == maelstrom && a.Cycle == nextThargoidCycle)
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstromHistoricalSummary> maelstromHistoricalSummaries = await dbContext.ThargoidMaelstromHistoricalSummaries
                .AsNoTracking()
                .Where(t => t.State != StarSystemThargoidLevelState.Titan && t.Maelstrom == maelstrom)
                .Include(t => t.Cycle)
                .Include(t => t.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ToListAsync(cancellationToken);
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory = maelstromHistoricalSummaries.Select(m => new OverwatchOverviewMaelstromHistoricalSummary(m)).ToList();
            List<OverwatchThargoidCycle> thargoidCycles = await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken);
            return new OverwatchMaelstromDetail(maelstrom, resultStarSystems, alertPredictions, maelstromHistory, thargoidCycles);
        }
    }
}
