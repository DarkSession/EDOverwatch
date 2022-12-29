namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemDetail : OverwatchStarSystem
    {
        public long PopulationOriginal { get; }
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

        protected OverwatchStarSystemDetail(
            StarSystem starSystem,
            decimal effortFocus,
            List<EDDatabase.WarEffort> warEfforts,
            List<FactionOperation> factionOperationDetails,
            List<Station> stations,
            List<StarSystemThargoidLevelProgress> starSystemThargoidLevelProgress
            ) :
            base(starSystem, effortFocus, 0, new(), 0, 0, 0)
        {
            PopulationOriginal = starSystem.OriginalPopulation;
            WarEfforts = warEfforts.Select(w => new OverwatchStarSystemWarEffort(w)).ToList();
            FactionOperations = factionOperationDetails.Count;
            FactionOperationDetails = factionOperationDetails;
            Stations = stations.Select(s => new OverwatchStation(s)).ToList();
            StationsUnderRepair = stations.Where(s => s.State == StationState.UnderRepairs).Count();
            StationsDamaged = stations.Where(s => s.State == StationState.Damaged).Count();
            StationsUnderAttack = stations.Where(s => s.State == StationState.UnderAttack).Count();
            LastTickTime = WeeklyTick.GetLastTick();
            LastTickDate = DateOnly.FromDateTime(LastTickTime.DateTime);
            ProgressDetails = starSystemThargoidLevelProgress.Select(s => new OverwatchStarSystemDetailProgress(s)).ToList();
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
                decimal effortFocus = 0;
                if (totalEffortSums.Any())
                {
                    Dictionary<WarEffortTypeGroup, long> systemEffortSums = new();
                    var systemEfforts = await dbContext.WarEfforts
                        .AsNoTracking()
                        .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) && w.StarSystem == starSystem)
                        .GroupBy(w => new
                        {
                            w.Type,
                            w.Source,
                        })
                        .Select(w => new
                        {
                            w.Key.Type,
                            w.Key.Source,
                            Amount = w.Sum(g => g.Amount),
                        })
                        .ToListAsync(cancellationToken);
                    foreach (var systemEffort in systemEfforts
                        .GroupBy(e => e.Type)
                        .Select(e => new
                        {
                            e.Key,
                            Amount = e.Sum(g => g.Amount),
                        }))
                    {
                        if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(systemEffort.Key, out WarEffortTypeGroup group))
                        {
                            if (!systemEffortSums.ContainsKey(group))
                            {
                                systemEffortSums[group] = systemEffort.Amount;
                                continue;
                            }
                            systemEffortSums[group] += systemEffort.Amount;
                        }
                    }
                    if (systemEffortSums.Any())
                    {
                        foreach (KeyValuePair<WarEffortTypeGroup, long> effort in totalEffortSums)
                        {
                            if (systemEffortSums.TryGetValue(effort.Key, out long amount) && amount > 0)
                            {
                                effortFocus += ((decimal)amount / (decimal)effort.Value / (decimal)systemEffortSums.Count);
                            }
                        }
                        effortFocus = Math.Round(effortFocus, 2);
                    }
                }

                List<EDDatabase.WarEffort> warEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)) && w.StarSystem == starSystem)
                    .OrderByDescending(w => w.Date)
                    .ToListAsync(cancellationToken);

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

                List<Station> stations = await dbContext.Stations
                    .AsNoTracking()
                    .Include(s => s.Type)
                    .Where(s => s.StarSystem == starSystem && StationTypes.Contains(s.Type!.Name) && s.State != StationState.Normal)
                    .ToListAsync(cancellationToken);

                List<StarSystemThargoidLevelProgress> starSystemThargoidLevelProgress = await dbContext.StarSystemThargoidLevelProgress
                    .AsNoTracking()
                    .Include(s => s.ThargoidLevel)
                    .Where(s => s.ThargoidLevel!.StarSystem == starSystem && s.Updated >= DateTimeOffset.UtcNow.AddDays(-7))
                    .OrderByDescending(s => s.Updated)
                    .ToListAsync(cancellationToken);

                return new OverwatchStarSystemDetail(starSystem, effortFocus, warEfforts, factionOperations, stations, starSystemThargoidLevelProgress);
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
            State = new(starSystemThargoidLevelProgress.ThargoidLevel?.State ?? throw new Exception("ThargoidLevel cannot be null"));
            Date = DateOnly.FromDateTime(starSystemThargoidLevelProgress.Updated.DateTime);
            DateTime = starSystemThargoidLevelProgress.Updated;
            Progress = starSystemThargoidLevelProgress.Progress ?? 0;
            ProgressPercentage = (decimal)(starSystemThargoidLevelProgress.Progress ?? 0m) / 100m;
        }
    }
}
