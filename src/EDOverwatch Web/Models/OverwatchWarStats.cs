namespace EDOverwatch_Web.Models
{
    public class OverwatchWarStats
    {
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

        public static async Task<OverwatchWarStats> Create(EdDbContext dbContext, CancellationToken cancellationToken)
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

            List<StatsCompletdSystemsPerCycle> completedSystemsPerCycle = await dbContext.StarSystemThargoidLevels
                .AsNoTracking()
                .Include(s => s.CycleEnd)
                .Where(s =>
                    s.Progress == 100 &&
                    (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled))
                .GroupBy(s => new
                {
                    s.CycleEndId,
                    s.State,
                })
                .Select(s => new StatsCompletdSystemsPerCycle(s.Key.CycleEndId, thargoidCycles, s.Key.State, s.Count()))
                .ToListAsync(cancellationToken);

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

                int maelstroms = await dbContext.ThargoidMaelstroms.CountAsync(cancellationToken);
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
                };

                List<WarEffortType> recoveryTypes = new()
                {
                    WarEffortType.Recovery,
                    WarEffortType.ThargoidProbeCollection,
                };

                statsHumans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeKills.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Humans && w.type == WarEffortType.Rescue)?.amount,
                    warEfforts.FirstOrDefault(w => w.side == WarEffortSide.Humans && w.type == WarEffortType.SupplyDelivery)?.amount,
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && warEffortTypeMissions.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0),
                    warEfforts.Where(w => w.side == WarEffortSide.Humans && recoveryTypes.Contains(w.type)).DefaultIfEmpty().Sum(s => s?.amount ?? 0)
                );
            }

            OverwatchOverviewContested statsContested = new(
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Invasion)?.Count ?? 0,
                systemThargoidLevelCount.FirstOrDefault(s => s.Key == StarSystemThargoidLevelState.Alert)?.Count ?? 0,
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled && s.ThargoidLevel!.Progress > 0).CountAsync(cancellationToken),
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
        public DateOnly Cycle { get; }
        public int Completed { get; }
        public OverwatchThargoidLevel State { get; }

        public StatsCompletdSystemsPerCycle(int? cycleEndId, List<OverwatchThargoidCycle> thargoidCycles, StarSystemThargoidLevelState level, int completed)
        {
            OverwatchThargoidCycle? overwatchThargoidCycle = thargoidCycles.FirstOrDefault(t => t.Id == cycleEndId);
            Cycle = DateOnly.FromDateTime((overwatchThargoidCycle?.Start ?? WeeklyTick.GetLastTick()).DateTime);
            State = new(level);
            Completed = completed;
        }
    }
}
