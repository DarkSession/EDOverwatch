﻿using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystems
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>()
            .Where(s => s > StarSystemThargoidLevelState.None)
            .Select(s => new OverwatchThargoidLevel(s))
            .ToList();
        public List<OverwatchStarSystem> Systems { get; } = new();
        public DateTimeOffset NextTick => WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 1);

        public OverwatchStarSystems(List<ThargoidMaelstrom> thargoidMaelstroms)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
        }

        private const string CacheKey = "OverwatchStarSystems";

        public static void DeleteMemoryEntry(IMemoryCache memoryCache)
        {
            memoryCache.Remove(CacheKey);
        }

        public static Task<OverwatchStarSystems> Create(EdDbContext dbContext, IMemoryCache memoryCache, CancellationToken cancellationToken)
        {
            return memoryCache.GetOrCreateAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchStarSystems> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

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
                    OdysseySettlements = s.Stations!.Any(s => s.Type!.Name == StationType.OdysseySettlementType),
                    FederalFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Federation),
                    EmpireFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Empire),
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

            OverwatchStarSystems result = new(maelstroms);
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
                result.Systems.Add(new OverwatchStarSystemFull(
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
                    system.EmpireFaction));
            }
            return result;
        }
    }
}
