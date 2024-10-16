﻿using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchWarStats
    {
        public static readonly DateTimeOffset CycleZero = new(2022, 11, 24, 7, 0, 0, TimeSpan.Zero);

        public OverwatchOverviewHuman Humans { get; set; }
        public OverwatchWarStatsThargoids Thargoids { get; set; }
        public OverwatchOverviewContested Contested { get; set; }
        public List<OverwatchOverviewMaelstromHistoricalSummary> MaelstromHistory { get; }
        public List<WarEffortSummary> WarEffortSums { get; }
        public List<StatsCompletdSystemsPerCycle> CompletdSystemsPerCycles { get; }
        public List<OverwatchThargoidCycle> ThargoidCycles { get; }

        protected OverwatchWarStats(
            OverwatchOverviewHuman statsHumans,
            OverwatchWarStatsThargoids statsThargoids,
            OverwatchOverviewContested statsContested,
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory,
            List<WarEffortSummary> warEffortSummaries,
            List<StatsCompletdSystemsPerCycle> completdSystemsPerCycles,
            List<OverwatchThargoidCycle> thargoidCycles)
        {
            Humans = statsHumans;
            Thargoids = statsThargoids;
            Contested = statsContested;
            MaelstromHistory = maelstromHistory;
            WarEffortSums = warEffortSummaries;
            CompletdSystemsPerCycles = completdSystemsPerCycles;
            ThargoidCycles = thargoidCycles;
        }

        private const string CacheKey = "OverwatchWarStats";
        public static void DeleteMemoryEntry(IAppCache appCache)
        {
            appCache.Remove(CacheKey);
        }

        public static Task<OverwatchWarStats> Create(EdDbContext dbContext, IAppCache appCache, CancellationToken cancellationToken)
        {
            return appCache.GetOrAddAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchWarStats> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<OverwatchThargoidCycle> thargoidCycles = await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken);

            List<WarEffortSummary> warEffortSums;
            {
                var warEffortCycleSums = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Include(w => w.Cycle)
                    .Where(w => w.Cycle != null && w.Side == WarEffortSide.Humans && w.Date > new DateOnly(2022, 11, 15))
                    .GroupBy(w => new { w.CycleId, w.Type })
                    .Select(w => new
                    {
                        w.Key.CycleId,
                        w.Key.Type,
                        Sum = w.Sum(x => x.Amount)
                    })
                    .ToListAsync(cancellationToken);

                warEffortSums = warEffortCycleSums
                    .Where(w => thargoidCycles.Any(t => t.Id == w.CycleId))
                    .Select(w =>
                    {
                        OverwatchThargoidCycle thargoidCycle = thargoidCycles.First(t => t.Id == w.CycleId);
                        return new WarEffortSummary(thargoidCycle.StartDate, w.Type, w.Sum);
                    })
                    .ToList();
            }

            List<StatsCompletdSystemsPerCycle> completedSystemsPerCycle = new();
            {
                List<StatsCompletdSystemsPerCycle> completedNormalSystemStates = await dbContext.StarSystemThargoidLevels
                    .AsNoTracking()
                    .Include(s => s.CycleEnd)
                    .Where(s =>
                        s.CurrentProgress!.IsCompleted &&
                        (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled))
                    .GroupBy(s => new
                    {
                        s.CycleEndId,
                        s.State,
                    })
                    .Select(s => new StatsCompletdSystemsPerCycle(s.Key.CycleEndId, thargoidCycles, s.Key.State, s.Count()))
                    .ToListAsync(cancellationToken);

                completedSystemsPerCycle.AddRange(completedNormalSystemStates);
            }
            {
                List<StatsCompletdSystemsPerCycle> completedTitansPerCycle = await dbContext.StarSystemThargoidLevels
                    .AsNoTracking()
                    .Include(s => s.Maelstrom)
                    .Where(s =>
                        s.CurrentProgress!.IsCompleted &&
                        s.State == StarSystemThargoidLevelState.Titan)
                    .GroupBy(s => new
                    {
                        s.Maelstrom!.DefeatCycleId,
                        s.State,
                    })
                    .Select(s => new StatsCompletdSystemsPerCycle(s.Key.DefeatCycleId, thargoidCycles, s.Key.State, s.Count()))
                    .ToListAsync(cancellationToken);

                completedSystemsPerCycle.AddRange(completedTitansPerCycle);
            }

            List<ThargoidMaelstromHistoricalSummary> maelstromHistoricalSummaries = await dbContext.ThargoidMaelstromHistoricalSummaries
                .AsNoTracking()
                .Where(t => t.State != StarSystemThargoidLevelState.Titan)
                .Include(t => t.Cycle)
                .Include(t => t.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ToListAsync(cancellationToken);
            List<OverwatchOverviewMaelstromHistoricalSummary> maelstromHistory = maelstromHistoricalSummaries.Select(m => new OverwatchOverviewMaelstromHistoricalSummary(m)).ToList();

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

            OverwatchOverviewHuman statsHumans;
            OverwatchWarStatsThargoids statsThargoids;

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

                int systemsControllingPreviouslyPopulated = await dbContext.StarSystems
                    .Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled && s.OriginalPopulation > 0)
                    .CountAsync(cancellationToken);

                int maelstroms = await dbContext.ThargoidMaelstroms.Where(t => t.HeartsRemaining > 0).CountAsync(cancellationToken);
                statsThargoids = new(
                    Math.Round((double)(thargoidsSystemsControlling + maelstroms) / (double)relevantSystemCount, 4),
                    maelstroms,
                    thargoidsSystemsControlling,
                    systemsControllingPreviouslyPopulated,
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

                List<WarEffortType> rescueTypes = new()
                {
                    WarEffortType.Rescue,
                    WarEffortType.EvacuationPassenger,
                    WarEffortType.EvacuationWounded,
                    WarEffortType.EvacuatioRefugee,
                    WarEffortType.ThargoidBiostorageCapsule,
                };

                List<WarEffortType> warEffortTypeMissions = new()
                {
                    WarEffortType.MissionCompletionGeneric,
                    WarEffortType.MissionCompletionDelivery,
                    WarEffortType.MissionCompletionRescue,
                    WarEffortType.MissionCompletionThargoidKill,
                    WarEffortType.MissionCompletionPassengerEvacuation,
                    WarEffortType.MissionCompletionRefugeeEvacuation,
                    WarEffortType.MissionCompletionSettlementReboot,
                    WarEffortType.MissionCompletionThargoidControlledSettlementReboot,
                    WarEffortType.MissionThargoidSpireSiteCollectResources,
                    WarEffortType.MissionThargoidSpireSiteSabotage,
                };

                List<WarEffortType> recoveryTypes = new()
                {
                    WarEffortType.Recovery,
                    WarEffortType.ThargoidProbeCollection,
                };

                List<WarEffortType> samplingTypes = new()
                {
                    WarEffortType.TissueSampleScout,
                    WarEffortType.TissueSampleCyclops,
                    WarEffortType.TissueSampleBasilisk,
                    WarEffortType.TissueSampleMedusa,
                    WarEffortType.TissueSampleHydra,
                    WarEffortType.TissueSampleOrthrus,
                    WarEffortType.TissueSampleGlaive,
                    WarEffortType.TissueSampleScythe,
                    WarEffortType.TissueSampleTitan,
                    WarEffortType.TissueSampleTitanMaw,
                    WarEffortType.ProtectiveMembraneScrap,
                };

                statsHumans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeKills.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && rescueTypes.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Humans && w.type == WarEffortType.SupplyDelivery)?.amount,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeMissions.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && recoveryTypes.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && samplingTypes.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0)
                );
            }

            OverwatchOverviewContested statsContested = new(
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Invasion)?.Count ?? 0,
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Alert)?.Count ?? 0,
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled && s.ThargoidLevel!.CurrentProgress!.HasProgress).CountAsync(cancellationToken),
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Recovery)?.Count ?? 0
            );

            OverwatchWarStats result = new(statsHumans, statsThargoids, statsContested, maelstromHistory, warEffortSums, completedSystemsPerCycle, thargoidCycles);
            return result;
        }
    }

    public class OverwatchWarStatsThargoids : OverwatchOverviewThargoids
    {
        public int SystemsControllingPreviouslyPopulated { get; }
        public OverwatchWarStatsThargoids(
            double controllingPercentage, int activeMaelstroms, int systemsControlling, int systemsControllingPreviouslyPopulated, long commanderKills, long refugeePopulation) :
            base(controllingPercentage, activeMaelstroms, systemsControlling, commanderKills, refugeePopulation)
        {
            SystemsControllingPreviouslyPopulated = systemsControllingPreviouslyPopulated;
        }
    }

    public class WarEffortSummary
    {
        public int CycleNumber { get; }
        public DateOnly Date { get; }
        public WarEffortType TypeId { get; }
        public string Type => EnumUtil.GetEnumMemberValue(TypeId);
        public string TypeGroup { get; }
        public long Amount { get; }

        public WarEffortSummary(DateTimeOffset dateTimeOffset, WarEffortType typeId, long amount) :
            this(DateOnly.FromDateTime(dateTimeOffset.DateTime), typeId, amount)
        {
        }

        public WarEffortSummary(DateOnly date, WarEffortType typeId, long amount)
        {
            CycleNumber = (date.DayNumber - DateOnly.FromDateTime(OverwatchWarStats.CycleZero.Date).DayNumber) / 7;
            Date = date;
            TypeId = typeId;
            Amount = amount;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(typeId, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
        }
    }

    public class StatsCompletdSystemsPerCycle
    {
        public int CycleNumber { get; }
        public DateOnly Cycle { get; }
        public int Completed { get; }
        public OverwatchThargoidLevel State { get; }

        public StatsCompletdSystemsPerCycle(int? cycleEndId, List<OverwatchThargoidCycle> thargoidCycles, StarSystemThargoidLevelState level, int completed)
        {
            OverwatchThargoidCycle? overwatchThargoidCycle = thargoidCycles.FirstOrDefault(t => t.Id == cycleEndId);
            if (overwatchThargoidCycle is not null)
            {
                CycleNumber = (int)(overwatchThargoidCycle.Start - OverwatchWarStats.CycleZero).TotalDays / 7;
            }
            Cycle = DateOnly.FromDateTime((overwatchThargoidCycle?.Start ?? WeeklyTick.GetLastTick()).DateTime);
            State = new(level);
            Completed = completed;
        }
    }
}
