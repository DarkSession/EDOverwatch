using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchOverview
    {
        public OverwatchOverviewHuman? Humans { get; set; }
        public OverwatchOverviewThargoids? Thargoids { get; set; }
        public OverwatchOverviewContested? Contested { get; set; }
        public List<OverwatchOverviewMaelstromHistoricalSummary> MaelstromHistory { get; set; } = new();
        public List<OverwatchThargoidCycle> ThargoidCycles { get; }

        protected OverwatchOverview(List<OverwatchThargoidCycle> thargoidCycles)
        {
            ThargoidCycles = thargoidCycles;
        }

        private const string CacheKey = "OverwatchOverview";

        public static void DeleteMemoryEntry(IMemoryCache memoryCache)
        {
            memoryCache.Remove(CacheKey);
        }

        public static Task<OverwatchOverview> Create(EdDbContext dbContext, IMemoryCache memoryCache, CancellationToken cancellationToken)
        {
            return memoryCache.GetOrCreateAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchOverview> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchOverview result = new(await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken));

            var systemThargoidLevelCount = await dbContext.StarSystems
                .Where(s =>
                    s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion ||
                    s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert ||
                    s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled ||
                    s.ThargoidLevel!.State == StarSystemThargoidLevelState.Titan ||
                    s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recovery)
                .GroupBy(s => s.ThargoidLevel!.State)
                .Select(s => new
                {
                    s.Key,
                    Count = s.Count(),
                })
                .ToListAsync(cancellationToken);

            int relevantSystemCount = await dbContext.StarSystems
                .Where(s =>
                    s.WarRelevantSystem &&
                    (s.Population > 0 ||
                        (s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled ||
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Titan)))
                .CountAsync(cancellationToken);
            if (relevantSystemCount == 0)
            {
                relevantSystemCount = 1;
            }

            var warEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w =>
                        w.StarSystem!.WarRelevantSystem &&
                        w.StarSystem!.ThargoidLevel != null)
                .GroupBy(w => new { w.Type, w.Side })
                .Select(w => new
                {
                    side = w.Key.Side,
                    type = w.Key.Type,
                    amount = w.Sum(s => s.Amount)
                })
                .ToListAsync(cancellationToken);

            {
                int thargoidsSystemsControlling = systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Controlled)?.Count ?? 0;

                long refugeePopulation = await dbContext.StarSystems
                    .AsNoTracking()
                    .Where(s => s.WarRelevantSystem && s.WarAffected && s.PopulationMin < s.OriginalPopulation)
                    .Select(s => s.OriginalPopulation - s.PopulationMin)
                    .SumAsync(cancellationToken);

                int maelstroms = await dbContext.ThargoidMaelstroms.CountAsync(cancellationToken);
                result.Thargoids = new(
                    Math.Round((double)(thargoidsSystemsControlling + maelstroms) / (double)relevantSystemCount, 4),
                    maelstroms,
                    thargoidsSystemsControlling,
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Thargoids && w.type == WarEffortType.KillGeneric)?.amount ?? 0,
                    refugeePopulation
                );
            }
            {
                int humansSystemsControlling = await dbContext.StarSystems
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Controlled &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Titan &&
                        s.Population > 0)
                    .CountAsync(cancellationToken);

                List<WarEffortType> warEffortTypeKills = new()
                {
                    WarEffortType.KillGeneric,
                    WarEffortType.KillThargoidScout,
                    WarEffortType.KillThargoidCyclops,
                    WarEffortType.KillThargoidBasilisk,
                    WarEffortType.KillThargoidMedusa,
                    WarEffortType.KillThargoidHydra,
                    WarEffortType.KillThargoidOrthrus,
                    WarEffortType.KillThargoidHunter,
                    WarEffortType.KillThargoidRevenant,
                    WarEffortType.KillThargoidBanshee,
                };

                List<WarEffortType> warEffortTypeMissions = new()
                {
                    WarEffortType.MissionCompletionGeneric,
                    WarEffortType.MissionCompletionDelivery,
                    WarEffortType.MissionCompletionRescue,
                    WarEffortType.MissionCompletionThargoidKill,
                    WarEffortType.MissionCompletionPassengerEvacuation,
                    WarEffortType.MissionCompletionSettlementReboot,
                    WarEffortType.MissionCompletionThargoidControlledSettlementReboot,
                    WarEffortType.MissionThargoidSpireSiteCollectResources,
                    WarEffortType.MissionThargoidSpireSiteSabotage,
                };

                List<WarEffortType> recoveryTypes = new()
                {
                    WarEffortType.Recovery,
                    WarEffortType.ThargoidProbeCollection,
                    WarEffortType.TissueSampleScout,
                    WarEffortType.TissueSampleCyclops,
                    WarEffortType.TissueSampleBasilisk,
                    WarEffortType.TissueSampleMedusa,
                    WarEffortType.TissueSampleHydra,
                    WarEffortType.TissueSampleOrthrus,
                    WarEffortType.TissueSampleGlaive,
                    WarEffortType.TissueSampleTitan,
                    WarEffortType.TissueSampleTitanMaw,
                    WarEffortType.ProtectiveMembraneScrap,
                    WarEffortType.ThargoidBiostorageCapsule,
                    WarEffortType.ThargoidImpureSpireMineral,
                    WarEffortType.ThargoidSemiRefinedSpireMineral,
                };

                result.Humans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeKills.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Humans && w.type == WarEffortType.Rescue)?.amount,
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Humans && w.type == WarEffortType.SupplyDelivery)?.amount,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeMissions.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && recoveryTypes.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    null
                );
            }

            result.Contested = new(
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Invasion)?.Count ?? 0,
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Alert)?.Count ?? 0,
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled && s.ThargoidLevel!.Progress > 0).CountAsync(cancellationToken),
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Recovery)?.Count ?? 0
            );

            List<ThargoidMaelstromHistoricalSummary> maelstromHistoricalSummaries = await dbContext.ThargoidMaelstromHistoricalSummaries
                .AsNoTracking()
                .Where(t => t.State != StarSystemThargoidLevelState.Titan)
                .Include(t => t.Cycle)
                .Include(t => t.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ToListAsync(cancellationToken);
            result.MaelstromHistory.AddRange(maelstromHistoricalSummaries.Select(m => new OverwatchOverviewMaelstromHistoricalSummary(m)));

            return result;
        }
    }

    public class OverwatchOverviewMaelstromHistoricalSummary
    {
        public OverwatchThargoidCycle Cycle { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel State { get; }
        public int Amount { get; }

        public OverwatchOverviewMaelstromHistoricalSummary(ThargoidMaelstromHistoricalSummary historicalSummary)
        {
            Cycle = new(historicalSummary.Cycle ?? throw new Exception("Cycle cannot be null"));
            Maelstrom = new(historicalSummary.Maelstrom ?? throw new Exception("Maelstrom cannot be null"));
            State = new(historicalSummary.State);
            Amount = historicalSummary.Amount;
        }
    }
}
