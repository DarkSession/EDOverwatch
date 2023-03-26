namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemDetail : OverwatchStarSystem
    {
        public DateTimeOffset LastTickTime { get; }
        public DateOnly LastTickDate { get; }
        public List<OverwatchStarSystemWarEffort> WarEfforts { get; }
        public List<OverwatchStarSystemDetailProgress> ProgressDetails { get; }
        public List<FactionOperation> FactionOperationDetails { get; }
        public List<OverwatchStation> Stations { get; }
        public List<OverwatchStarSystemWarEffortType> WarEffortTypes => Enum.GetValues<WarEffortType>()
            .Select(w => new OverwatchStarSystemWarEffortType(w))
            .ToList();
        public List<OverwatchStarSystemWarEffortSource> WarEffortSources => Enum.GetValues<WarEffortSource>()
            .Select(w => new OverwatchStarSystemWarEffortSource(w))
            .ToList();
        public List<OverwatchStarSystemThargoidLevelHistory> StateHistory { get; }
        public List<OverwatchStarSystemWarEffortCycle> WarEffortSummaries { get; }
        public List<DateOnly> DaysSincePreviousTick { get; }

        protected OverwatchStarSystemDetail(
            StarSystem starSystem,
            decimal effortFocus,
            List<OverwatchStarSystemWarEffort> warEfforts,
            List<FactionOperation> factionOperationDetails,
            List<Station> stations,
            List<StarSystemThargoidLevelProgress> starSystemThargoidLevelProgress,
            List<StarSystemThargoidLevel> stateHistory,
            List<OverwatchStarSystemWarEffortCycle> warEffortSummaries,
            List<DateOnly> daysSincePreviousTick,
            List<Station> rescueShips
            ) :
            base(starSystem, effortFocus, 0, 0, 0, 0, new(), 0, 0, 0)
        {
            WarEfforts = warEfforts;
            FactionOperations = factionOperationDetails.Count;
            FactionOperationDetails = factionOperationDetails;
            Stations = stations.Select(s => new OverwatchStation(s, rescueShips, starSystem.ThargoidLevel!)).ToList();
            StationsUnderRepair = stations.Where(s => s.State == StationState.UnderRepairs).Count();
            StationsDamaged = stations.Where(s => s.State == StationState.Damaged).Count();
            StationsUnderAttack = stations.Where(s => s.State == StationState.UnderAttack).Count();
            LastTickTime = WeeklyTick.GetLastTick();
            LastTickDate = DateOnly.FromDateTime(LastTickTime.DateTime);
            ProgressDetails = starSystemThargoidLevelProgress.Select(s => new OverwatchStarSystemDetailProgress(s)).ToList();
            StateHistory = stateHistory.Select(s => new OverwatchStarSystemThargoidLevelHistory(s)).ToList();
            WarEffortSummaries = warEffortSummaries;
            DaysSincePreviousTick = daysSincePreviousTick;
        }

        public static async Task<OverwatchStarSystemDetail?> Create(long systemAddress, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            StarSystem? starSystem = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .FirstOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem?.ThargoidLevel != null)
            {
                Dictionary<WarEffortTypeGroup, long> totalEffortSums = await WarEffort.GetTotalWarEfforts(dbContext, cancellationToken);
                DateOnly startDate = WarEffort.GetWarEffortFocusStartDate();

                decimal effortFocus = 0;
                if (totalEffortSums.Any())
                {
                    List<WarEffortTypeSum> systemEfforts = await dbContext.WarEfforts
                        .AsNoTracking()
                        .Where(w =>
                                w.Date >= startDate &&
                                w.StarSystem == starSystem &&
                                w.Side == WarEffortSide.Humans)
                        .GroupBy(w => new
                        {
                            w.Type,
                        })
                        .Select(w => new WarEffortTypeSum(w.Key.Type, w.Sum(g => g.Amount)))
                        .ToListAsync(cancellationToken);
                    effortFocus = WarEffort.CalculateSystemFocus(systemEfforts, totalEffortSums);
                }

                DateTimeOffset previousTickTime = WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, -1);
                DateOnly previousTickDay = DateOnly.FromDateTime(previousTickTime.DateTime);
                DateOnly lastTickDay = DateOnly.FromDateTime(WeeklyTick.GetTickTime(DateTimeOffset.UtcNow, 0).DateTime);
                DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime);

                List<DateOnly> daysSincePreviousTick = Enumerable.Range(0, today.DayNumber - previousTickDay.DayNumber + 1)
                    .Select(previousTickDay.AddDays)
                    .ToList();

                List<OverwatchStarSystemWarEffort> warEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w =>
                            w.Date >= previousTickDay &&
                            w.StarSystem == starSystem &&
                            w.Side == WarEffortSide.Humans)
                    .OrderByDescending(w => w.Date)
                    .GroupBy(w => new
                    {
                        w.Date,
                        w.Source,
                        w.Type,
                    })
                    .Select(w => new OverwatchStarSystemWarEffort(w.Key.Date, w.Key.Type, w.Key.Source, w.Sum(f => f.Amount)))
                    .ToListAsync(cancellationToken);

                List<OverwatchStarSystemWarEffortCycle> warEffortSummaries = new();

                {
                    List<OverwatchStarSystemWarEffortCycleEntry> lastTickWarEfforts = warEfforts
                        .Where(w => w.Date >= lastTickDay)
                        .GroupBy(w => new
                        {
                            w.WarEffortSource,
                            w.WarEffortType,
                        })
                        .Select(w => new OverwatchStarSystemWarEffortCycleEntry(w.Key.WarEffortType, w.Key.WarEffortSource, w.Sum(w => w.Amount)))
                        .ToList();
                    warEffortSummaries.Add(new(lastTickDay, lastTickDay.AddDays(7), lastTickWarEfforts));
                }
                {
                    List<OverwatchStarSystemWarEffortCycleEntry> previousTickWarEfforts = warEfforts
                        .Where(w => w.Date >= previousTickDay && w.Date < lastTickDay)
                        .GroupBy(w => new
                        {
                            w.WarEffortSource,
                            w.WarEffortType,
                        })
                        .Select(w => new OverwatchStarSystemWarEffortCycleEntry(w.Key.WarEffortType, w.Key.WarEffortSource, w.Sum(w => w.Amount)))
                        .ToList();
                    warEffortSummaries.Add(new(previousTickDay, lastTickDay, previousTickWarEfforts));
                }

                List<FactionOperation> factionOperations;
                {
                    List<DcohFactionOperation> dcohFactionOperation = await dbContext.DcohFactionOperations
                        .AsNoTracking()
                        .Include(d => d.Faction)
                        .Include(d => d.StarSystem)
                        .Where(s => s.StarSystem == starSystem && s.Status == DcohFactionOperationStatus.Active)
                        .ToListAsync(cancellationToken);
                    factionOperations = dcohFactionOperation.Select(d => new FactionOperation(d)).ToList();
                }

                IQueryable<Station> stationQuery = dbContext.Stations
                    .AsNoTracking()
                    .Include(s => s.Type)
                    .Include(s => s.Body)
                    .Include(s => s.StarSystem)
                    .Include(s => s.MinorFaction)
                    .ThenInclude(m => m!.Allegiance)
                    .Include(s => s.PriorMinorFaction)
                    .ThenInclude(m => m!.Allegiance)
                    .Where(s =>
                        s.StarSystem == starSystem &&
                        StationTypes.Contains(s.Type!.Name));
                /*
                if (starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Invasion)
                {
                    stationQuery = stationQuery.Where(s => s.State != StationState.Normal || (s.State == StationState.Normal && s.Updated >= WeeklyTick.GetLastTick()));
                }
                */

                List<Station> stations = await stationQuery
                    .ToListAsync(cancellationToken);

                List<Station> rescueShips = await dbContext.Stations
                    .AsNoTracking()
                    .Include(s => s.StarSystem)
                    .Include(s => s.MinorFaction)
                    .ThenInclude(m => m!.Allegiance)
                    .Where(s => s.IsRescueShip == RescueShipType.Primary)
                    .ToListAsync(cancellationToken);

                List<StarSystemThargoidLevelProgress> starSystemThargoidLevelProgress = await dbContext.StarSystemThargoidLevelProgress
                    .AsNoTracking()
                    .Include(s => s.ThargoidLevel)
                    .Where(s => s.ThargoidLevel!.StarSystem == starSystem && s.Updated >= previousTickTime)
                    .OrderByDescending(s => s.Updated)
                    .ToListAsync(cancellationToken);

                List<StarSystemThargoidLevel> thargoidLevelHistory = await dbContext.StarSystemThargoidLevels
                     .AsNoTracking()
                     .Include(s => s.CycleStart)
                     .Include(s => s.CycleEnd)
                     .Include(s => s.StateExpires)
                     .Where(s => s.StarSystem == starSystem && (s.CycleEnd == null || s.CycleStart!.Start <= s.CycleEnd.Start))
                     .ToListAsync(cancellationToken);

                return new OverwatchStarSystemDetail(
                    starSystem,
                    effortFocus,
                    warEfforts,
                    factionOperations,
                    stations,
                    starSystemThargoidLevelProgress,
                    thargoidLevelHistory,
                    warEffortSummaries,
                    daysSincePreviousTick,
                    rescueShips);
            }
            return null;
        }

        private static List<string> StationTypes { get; } = new()
        {
            "Bernal",
            "Orbis",
            "Coriolis",
            "CraterOutpost",
            "MegaShip",
            "Outpost",
            "CraterPort",
            "Ocellus",
            "AsteroidBase",
        };
    }

    public class OverwatchStarSystemWarEffortType
    {
        public int TypeId { get; }
        public string Name { get; set; }

        public OverwatchStarSystemWarEffortType(WarEffortType warEffortType)
        {
            TypeId = (int)warEffortType;
            Name = warEffortType.GetEnumMemberValue();
        }
    }

    public class OverwatchStarSystemWarEffortSource
    {
        public int SourceId { get; }
        public string Name { get; }
        public OverwatchStarSystemWarEffortSource(WarEffortSource warEffortSource)
        {
            SourceId = (int)warEffortSource;
            Name = warEffortSource.GetEnumMemberValue();
        }
    }

    public class OverwatchStarSystemDetailProgress
    {
        public OverwatchThargoidLevel State { get; }
        public DateOnly Date { get; }
        public DateTimeOffset DateTime { get; }
        public short Progress { get; }
        public decimal ProgressPercentage { get; }
        public OverwatchStarSystemDetailProgress(StarSystemThargoidLevelProgress starSystemThargoidLevelProgress)
        {
            State = new(starSystemThargoidLevelProgress.ThargoidLevel);
            Date = DateOnly.FromDateTime(starSystemThargoidLevelProgress.Updated.DateTime);
            DateTime = starSystemThargoidLevelProgress.Updated;
            Progress = starSystemThargoidLevelProgress.Progress ?? 0;
            ProgressPercentage = (decimal)(starSystemThargoidLevelProgress.Progress ?? 0m) / 100m;
        }
    }

    public class OverwatchStarSystemThargoidLevelHistory
    {
        public bool AllowDetailAnalysisDisplay { get; }
        public DateOnly AnalysisCycle { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public DateTimeOffset StateStart { get; }
        public DateTimeOffset? StateEnds { get; }
        public DateTimeOffset? StateIngameTimerExpires { get; }
        public short? Progress { get; }
        public decimal? ProgressPercentage { get; }

        public OverwatchStarSystemThargoidLevelHistory(StarSystemThargoidLevel starSystemThargoidLevel)
        {
            AllowDetailAnalysisDisplay = (
                starSystemThargoidLevel.Progress >= 100 &&
                (starSystemThargoidLevel.CycleEnd == null ||
                starSystemThargoidLevel.CycleEnd.Start >= new DateTimeOffset(2022, 12, 22, 7, 0, 0, TimeSpan.Zero)));
            AnalysisCycle = DateOnly.FromDateTime((starSystemThargoidLevel.CycleEnd?.Start ?? WeeklyTick.GetLastTick()).DateTime);
            ThargoidLevel = new(starSystemThargoidLevel);
            StateStart = starSystemThargoidLevel.CycleStart!.Start;
            StateEnds = starSystemThargoidLevel.CycleEnd?.End;
            StateIngameTimerExpires = starSystemThargoidLevel.StateExpires?.End;
            Progress = starSystemThargoidLevel.Progress;
            ProgressPercentage = starSystemThargoidLevel.Progress != null ? (decimal)starSystemThargoidLevel.Progress / 100m : null;
        }
    }
}
