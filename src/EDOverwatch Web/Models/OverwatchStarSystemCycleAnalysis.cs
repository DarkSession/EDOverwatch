namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemCycleAnalysis
    {
        public long SystemAddress { get; }
        public string SystemName { get; }
        public DateTimeOffset ProgressStart { get; }
        public DateTimeOffset ProgressCompleted { get; }
        public List<OverwatchStarSystemCycleAnalysisWarEffort> CycleWarEffortsUntilCompleted { get; }
        public OverwatchThargoidLevel ThargoidState { get; }

        protected OverwatchStarSystemCycleAnalysis(StarSystem starSystem, StarSystemThargoidLevel starSystemThargoidLevel, ThargoidCycle thargoidCycle, DateTimeOffset progressCompleted, List<OverwatchStarSystemCycleAnalysisWarEffort> cycleWarEffortsUntilCompleted)
        {
            SystemAddress = starSystem.SystemAddress;
            SystemName = starSystem.Name;
            ProgressStart = (starSystemThargoidLevel.State == StarSystemThargoidLevelState.Recovery) ?
                (starSystemThargoidLevel.CycleStart?.Start ?? throw new Exception("starSystemThargoidLevel.CycleStart cannot be null")) :
                thargoidCycle.Start;
            ProgressCompleted = progressCompleted;
            CycleWarEffortsUntilCompleted = cycleWarEffortsUntilCompleted;
            ThargoidState = new(starSystemThargoidLevel.State);
        }

        public static async Task<OverwatchStarSystemCycleAnalysis?> Create(long systemAddress, DateOnly cycle, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateTimeOffset cycleTime = WeeklyTick.GetTickTime(cycle);
            if (await dbContext.ThargoidCycles.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Start == cycleTime, cancellationToken) is ThargoidCycle thargoidCycle &&
                await dbContext.StarSystems.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken) is StarSystem starSystem
                )
            {
                IQueryable<StarSystemThargoidLevel> starSystemThargoidLevelQuery = dbContext.StarSystemThargoidLevels
                    .AsNoTracking()
                    .Include(s => s.CycleStart)
                    .Include(s => s.CycleEnd)
                    .Include(s => s.StateExpires)
                    .Include(s => s.ProgressHistory!.Where(p => p.Progress == 100))
                    .Where(s => s.StarSystem == starSystem);
                if (thargoidCycle.Start == WeeklyTick.GetLastTick())
                {
                    starSystemThargoidLevelQuery = starSystemThargoidLevelQuery.Where(s => s.CycleEnd == null);
                }
                else
                {
                    starSystemThargoidLevelQuery = starSystemThargoidLevelQuery.Where(s => s.CycleEnd == thargoidCycle && s.CycleStart!.Start <= s.CycleEnd.Start);
                }

                StarSystemThargoidLevel? historicalThargoidLevel = await starSystemThargoidLevelQuery.FirstOrDefaultAsync(cancellationToken);
                if (historicalThargoidLevel?.ProgressHistory != null &&
                    historicalThargoidLevel.ProgressHistory.Any() &&
                    historicalThargoidLevel.Progress >= 100 &&
                    (historicalThargoidLevel.CycleEnd == null ||
                    historicalThargoidLevel.CycleEnd.Start >= new DateTimeOffset(2022, 12, 22, 7, 0, 0, TimeSpan.Zero)))
                {
                    DateOnly cycleStartDate = DateOnly.FromDateTime(thargoidCycle.Start.DateTime);
                    DateTimeOffset progressCompleted = historicalThargoidLevel.ProgressHistory.OrderBy(p => p.Updated).Select(p => p.Updated).First();
                    List<OverwatchStarSystemCycleAnalysisWarEffort> warEfforts = await dbContext.WarEfforts
                        .AsNoTracking()
                        .Where(w => w.Date >= cycleStartDate && w.Date <= cycleStartDate && w.StarSystem == starSystem)
                        .GroupBy(w => new
                        {
                            w.Source,
                            w.Type,
                        })
                        .Select(w => new OverwatchStarSystemCycleAnalysisWarEffort(w.Key.Type, w.Key.Source, w.Sum(f => f.Amount)))
                        .ToListAsync(cancellationToken);

                    OverwatchStarSystemCycleAnalysis overwatchStarSystemCycleAnalysis = new(starSystem, historicalThargoidLevel, thargoidCycle, progressCompleted, warEfforts);
                    return overwatchStarSystemCycleAnalysis;
                }
            }
            return null;
        }
    }

    public class OverwatchStarSystemCycleAnalysisWarEffort
    {
        public string Type { get; }
        public int TypeId { get; }
        public string Source { get; }
        public int SourceId { get; }
        public string TypeGroup { get; }
        public long Amount { get; }

        public OverwatchStarSystemCycleAnalysisWarEffort(WarEffortType type, WarEffortSource source, long amount)
        {
            Type = type.GetEnumMemberValue();
            TypeId = (int)type;
            Source = source.GetEnumMemberValue();
            SourceId = (int)source;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(type, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
            Amount = amount;
        }
    }
}
