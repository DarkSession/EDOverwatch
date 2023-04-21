namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetail : OverwatchMaelstrom
    {
        public List<OverwatchStarSystem> Systems { get; }
        public List<OverwatchMaelstromDetailSystemAtRisk> SystemsAtRisk { get; }
        public List<OverwatchOverviewMaelstromHistoricalSummary> MaelstromHistory { get; set; } = new();
        public List<OverwatchThargoidCycle> ThargoidCycles { get; }

        protected OverwatchMaelstromDetail(
            ThargoidMaelstrom thargoidMaelstrom, List<OverwatchStarSystem> systems,
            List<OverwatchMaelstromDetailSystemAtRisk> systemAtRisks,
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory,
            List<OverwatchThargoidCycle> thargoidCycles) :
            base(thargoidMaelstrom)
        {
            Systems = systems;
            SystemsAtRisk = systemAtRisks;
            MaelstromHistory = maelstromHistory;
            ThargoidCycles = thargoidCycles;
        }

        public static async Task<OverwatchMaelstromDetail> Create(ThargoidMaelstrom maelstrom, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            await dbContext.Entry(maelstrom)
                .Reference(m => m.StarSystem)
                .LoadAsync(cancellationToken);
            if (maelstrom.StarSystem == null)
            {
                throw new Exception("StarSystem is null");
            }
            DateTimeOffset lastTick = WeeklyTick.GetLastTick();
            DateTimeOffset stationMaxAge = DateTimeOffset.UtcNow.AddDays(-1);
            if (lastTick < stationMaxAge)
            {
                stationMaxAge = lastTick;
            }

            var systems = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevel!.Maelstrom == maelstrom && s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .Include(s => s.ThargoidLevel!.CurrentProgress)
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
                    StationsUnderRepair = s.Stations!.Where(s => s.Updated > stationMaxAge && s.State == StationState.UnderRepairs).Count(),
                })
                .ToListAsync(cancellationToken);

            Dictionary<WarEffortTypeGroup, long> totalEffortSums = await WarEffort.GetTotalWarEfforts(dbContext, cancellationToken);
            DateOnly startDate = WarEffort.GetWarEffortFocusStartDate();

            var efforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w =>
                        w.Date >= startDate &&
                        systems.Select(y => y.StarSystem).Contains(w.StarSystem!) &&
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

            List<OverwatchStarSystem> resultStarSystems = new();
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
                resultStarSystems.Add(new OverwatchStarSystem(
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

            DateTimeOffset controlledMaxEnd = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, -3);
            DateTimeOffset alertInvasionMaxEnd = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, -1);
            DateTimeOffset nextTick = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);

            List<OverwatchMaelstromDetailSystemAtRisk> systemsAtRiskResult = new();
            decimal systemsAtRiskSphere = maelstrom.InfluenceSphere + 10.02m;
            List<StarSystem> systemsAtRisk = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => 
                    s.ThargoidLevelHistory!.Where(s => 
                        s.CycleEnd != null && 
                        s.CycleEnd.Start >= controlledMaxEnd && 
                        (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled)))
                .ThenInclude(t => t.CycleEnd)
                .Where(s =>
                    s.LocationX >= maelstrom.StarSystem.LocationX - systemsAtRiskSphere && s.LocationX <= maelstrom.StarSystem.LocationX + systemsAtRiskSphere &&
                    s.LocationY >= maelstrom.StarSystem.LocationY - systemsAtRiskSphere && s.LocationY <= maelstrom.StarSystem.LocationY + systemsAtRiskSphere &&
                    s.LocationZ >= maelstrom.StarSystem.LocationZ - systemsAtRiskSphere && s.LocationZ <= maelstrom.StarSystem.LocationZ + systemsAtRiskSphere &&
                    (s.ThargoidLevel == null || s.ThargoidLevel.State == StarSystemThargoidLevelState.None || (s.ThargoidLevel.State == StarSystemThargoidLevelState.Recovery && s.ThargoidLevel.StateExpires!.End <= nextTick)))
                .ToListAsync(cancellationToken);
            foreach (StarSystem systemAtRisk in systemsAtRisk.OrderBy(s => maelstrom.StarSystem.DistanceTo(s)))
            {
                double distance = Math.Round(maelstrom.StarSystem.DistanceTo(systemAtRisk), 2);
                if (distance > (double)systemsAtRiskSphere ||
                    systemAtRisk.ThargoidLevelHistory!.Any(t => t.CycleEnd!.Start >= alertInvasionMaxEnd) ||
                    systemAtRisk.ThargoidLevelHistory!.Any(t => t.State == StarSystemThargoidLevelState.Controlled && t.CycleEnd!.Start >= controlledMaxEnd))
                {
                    continue;
                }
                List<StarSystem> attackingSystems = systems.Where(s =>
                        ((s.StarSystem.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled &&
                        (s.StarSystem.ThargoidLevel!.Progress < 100 || s.StarSystem.ThargoidLevel!.Progress == null)) ||
                        s.StarSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Maelstrom) &&

                        s.StarSystem.DistanceTo(systemAtRisk) <= 10.02f)
                    .Select(s => s.StarSystem)
                    .ToList();
                if (attackingSystems.Any())
                {
                    systemsAtRiskResult.Add(new OverwatchMaelstromDetailSystemAtRisk(systemAtRisk.Name, distance, systemAtRisk.Population, attackingSystems));
                    if (systemsAtRiskResult.Count >= 20)
                    {
                        break;
                    }
                }
            }

            List<ThargoidMaelstromHistoricalSummary> maelstromHistoricalSummaries = await dbContext.ThargoidMaelstromHistoricalSummaries
                .AsNoTracking()
                .Where(t => t.State != StarSystemThargoidLevelState.Maelstrom && t.Maelstrom == maelstrom)
                .Include(t => t.Cycle)
                .Include(t => t.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ToListAsync(cancellationToken);
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory = maelstromHistoricalSummaries.Select(m => new OverwatchOverviewMaelstromHistoricalSummary(m)).ToList();
            List<OverwatchThargoidCycle> thargoidCycles = await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken);
            return new OverwatchMaelstromDetail(maelstrom, resultStarSystems, systemsAtRiskResult, maelstromHistory, thargoidCycles);
        }
    }
}
