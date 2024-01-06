using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemFullDetail : OverwatchStarSystemFull
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
        public List<OverwatchStarSystemNearbySystem> NearbySystems { get; }
        public List<DateOnly> DaysSincePreviousTick { get; }
        public OverwatchStarSystemAttackDefense AttackDefense { get; }

        protected OverwatchStarSystemFullDetail(
            StarSystem starSystem,
            decimal effortFocus,
            List<OverwatchStarSystemWarEffort> warEfforts,
            List<FactionOperation> factionOperationDetails,
            List<Station> stations,
            List<StarSystemThargoidLevelProgress> starSystemThargoidLevelProgress,
            List<StarSystemThargoidLevel> stateHistory,
            List<OverwatchStarSystemWarEffortCycle> warEffortSummaries,
            List<DateOnly> daysSincePreviousTick,
            List<Station> rescueShips,
            List<StarSystem> nearbySystems,
            StarSystem? recentAttacker,
            StarSystem? predictedAttacker,
            StarSystem? recentlyAttacked,
            StarSystem? predictedAttack,
            bool odysseySettlements,
            bool federalFaction,
            bool imperialFaction,
            bool axConflictZones) :
            base(starSystem, effortFocus, 0, 0, 0, 0, new(), 0, 0, 0, odysseySettlements, federalFaction, imperialFaction, axConflictZones, stations.Where(s => s.State == StationState.UnderAttack && (s.Type?.IsWarGroundAsset ?? false)).Any(), predictedAttacker != null)
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
            NearbySystems = nearbySystems.Select(n => new OverwatchStarSystemNearbySystem(n, starSystem)).OrderBy(n => n.Distance).ToList();
            DaysSincePreviousTick = daysSincePreviousTick;
            {
                int? requirementsTissueSampleTotal = EDWarProgressRequirements.WarEfforts.GetRequirementsEstimate((decimal)DistanceToMaelstrom, PopulationOriginal > 0, ThargoidLevel.Level);
                int? requirementsTissueSampleRemaining = requirementsTissueSampleTotal;
                int? titanPodsTotal = null;
                int? titanPodsRemaining = null;
                if (StateProgress.ProgressPercent is decimal progress)
                {
                    decimal remaining = 1m - progress;
                    if (requirementsTissueSampleTotal is not null)
                    {
                        requirementsTissueSampleRemaining = (int)Math.Ceiling((decimal)requirementsTissueSampleTotal * remaining);
                    }
                    if (titanPodsTotal is not null)
                    {
                        titanPodsRemaining = (int)Math.Ceiling((decimal)titanPodsTotal * remaining);
                    }
                }
                AttackDefense = new(recentAttacker, predictedAttacker, recentlyAttacked, predictedAttack, requirementsTissueSampleTotal, requirementsTissueSampleRemaining, titanPodsTotal, titanPodsRemaining);
            }
        }

        private static string CacheKey(long systemAddress)
        {
            return $"OverwatchStarSystemFullDetail-{systemAddress}";
        }

        public static void DeleteMemoryEntry(IAppCache appCache, long systemAddress)
        {
            appCache.Remove(CacheKey(systemAddress));
        }

        public static Task<OverwatchStarSystemFullDetail?> Create(long systemAddress, EdDbContext dbContext, IAppCache appCache, CancellationToken cancellationToken)
        {
            return appCache.GetOrAddAsync(CacheKey(systemAddress), (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return CreateInternal(systemAddress, dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchStarSystemFullDetail?> CreateInternal(long systemAddress, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateTimeOffset lastTick = WeeklyTick.GetLastTick();
            DateTimeOffset signalMaxAge = lastTick.AddDays(-7);

            var systemData = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .Include(s => s.ThargoidLevel!.StateExpires)
                .Include(s => s.ThargoidLevel!.CurrentProgress)
                .Include(s => s.ThargoidLevel!.ProgressHistory!.Where(p => p.IsCompleted))
                .Where(s => s.SystemAddress == systemAddress)
                .Select(s => new
                {
                    StarSystem = s,
                    OdysseySettlements = s.Stations!.Any(s => s.Type!.Name == StationType.OdysseySettlementType),
                    FederalFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Federation),
                    EmpireFaction = s.MinorFactionPresences!.Any(m => m.MinorFaction!.Allegiance!.Name == FactionAllegiance.Empire),
                    AXConflictZones = s.FssSignals!.Any(f => f.Type == StarSystemFssSignalType.AXCZ && f.LastSeen >= signalMaxAge),
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (systemData?.StarSystem?.ThargoidLevel != null)
            {
                ThargoidCycle currentCycle = await dbContext.GetThargoidCycle(cancellationToken);
                StarSystem starSystem = systemData.StarSystem;
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

                List<Station> stations = await dbContext.Stations
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
                        StationTypes.Contains(s.Type!.Name))
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

                StarSystem? recentAttacker = null;
                StarSystem? predictedAttacker = null;

                StarSystem? recentlyAttacked = null;
                StarSystem? predictedAttack = null;

                switch (starSystem.ThargoidLevel.State)
                {
                    case StarSystemThargoidLevelState.Alert:
                        {
                            recentAttacker = await dbContext.AlertPredictionCycleAttackers
                                .AsNoTracking()
                                .Include(a => a.AttackerStarSystem)
                                .Where(a => a.Cycle == currentCycle && a.VictimStarSystem == starSystem)
                                .Select(a => a.AttackerStarSystem)
                                .FirstOrDefaultAsync(cancellationToken);

                            break;
                        }
                    case StarSystemThargoidLevelState.Controlled:
                        {
                            ThargoidCycle nextCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

                            recentlyAttacked = await dbContext.AlertPredictionCycleAttackers
                                .AsNoTracking()
                                .Include(a => a.VictimStarSystem)
                                .Where(a => a.Cycle == currentCycle && a.AttackerStarSystem == starSystem)
                                .Select(a => a.VictimStarSystem)
                                .FirstOrDefaultAsync(cancellationToken);
                            predictedAttack = await dbContext.AlertPredictionAttackers
                                .AsNoTracking()
                                .Where(a => a.AlertPrediction!.AlertLikely && a.AlertPrediction.Cycle == nextCycle && a.StarSystem == starSystem)
                                .Select(a => a.AlertPrediction!.StarSystem)
                                .FirstOrDefaultAsync(cancellationToken);
                            break;
                        }
                    case StarSystemThargoidLevelState.None:
                    case StarSystemThargoidLevelState.Recovery:
                        {
                            ThargoidCycle nextCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

                            predictedAttacker = await dbContext.AlertPredictions
                                .AsNoTracking()
                                .Where(a => a.AlertLikely && a.Cycle == nextCycle && a.StarSystem == starSystem)
                                .SelectMany(a => a.Attackers!)
                                .OrderBy(a => a.Order)
                                .Select(a => a.StarSystem)
                                .FirstOrDefaultAsync(cancellationToken);

                            break;
                        }
                }

                List<StarSystem> nearbySystems;
                {
                    decimal maxDistance = 10m;
                    nearbySystems = await dbContext.StarSystems
                        .AsNoTracking()
                        .Include(s => s.ThargoidLevel)
                        .Where(s =>
                            s != starSystem &&
                            s.LocationX >= starSystem.LocationX - maxDistance && s.LocationX <= starSystem.LocationX + maxDistance &&
                            s.LocationY >= starSystem.LocationY - maxDistance && s.LocationY <= starSystem.LocationY + maxDistance &&
                            s.LocationZ >= starSystem.LocationZ - maxDistance && s.LocationZ <= starSystem.LocationZ + maxDistance)
                        .ToListAsync(cancellationToken);
                    nearbySystems = nearbySystems.Where(n => n.DistanceTo(starSystem) <= 10f && n.ThargoidLevel != null).ToList();
                }

                return new OverwatchStarSystemFullDetail(
                    starSystem,
                    effortFocus,
                    warEfforts,
                    factionOperations,
                    stations,
                    starSystemThargoidLevelProgress,
                    thargoidLevelHistory,
                    warEffortSummaries,
                    daysSincePreviousTick,
                    rescueShips,
                    nearbySystems,
                    recentAttacker: recentAttacker,
                    predictedAttacker: predictedAttacker,
                    recentlyAttacked: recentlyAttacked,
                    predictedAttack: predictedAttack,
                    systemData.OdysseySettlements,
                    systemData.FederalFaction,
                    systemData.EmpireFaction,
                    systemData.AXConflictZones);
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

    public class OverwatchStarSystemNearbySystem
    {
        public OverwatchStarSystem StarSystem { get; }
        public double Distance { get; }
        public double DistanceToTitan { get; }

        public OverwatchStarSystemNearbySystem(StarSystem nearbySystem, StarSystem starSystem)
        {
            StarSystem = new(nearbySystem, false);
            Distance = Math.Round(nearbySystem.DistanceTo(starSystem), 4);
            if (starSystem.ThargoidLevel?.Maelstrom?.StarSystem is not null)
            {
                DistanceToTitan = Math.Round(nearbySystem.DistanceTo(starSystem.ThargoidLevel.Maelstrom.StarSystem), 4);
            }
        }
    }

    public class OverwatchStarSystemAttackDefense
    {
        public OverwatchStarSystemMin? RecentAttacker { get; }
        public OverwatchStarSystemMin? PredictedAttacker { get; }
        public OverwatchStarSystemMin? RecentlyAttacked { get; }
        public OverwatchStarSystemMin? PredictedAttack { get; }
        public int? RequirementsTissueSampleTotal { get; }
        public int? RequirementsTissueSampleRemaining { get; }
        public int? RequirementsTitanPodsTotal { get; }
        public int? RequirementsTitanPodsRemaining { get; }

        public OverwatchStarSystemAttackDefense(StarSystem? recentAttacker, StarSystem? predictedAttacker, StarSystem? recentlyAttacked, StarSystem? predictedAttack, int? requirementsTissueSampleTotal, int? requirementsTissueSampleRemaining, int? requirementsTitanPodsTotal, int? requirementsTitanPodsRemaining)
        {
            if (recentAttacker != null)
            {
                RecentAttacker = new(recentAttacker);
            }
            if (predictedAttacker != null)
            {
                PredictedAttacker = new(predictedAttacker);
            }
            if (recentlyAttacked != null)
            {
                RecentlyAttacked = new(recentlyAttacked);
            }
            if (predictedAttack != null)
            {
                PredictedAttack = new(predictedAttack);
            }
            RequirementsTissueSampleTotal = requirementsTissueSampleTotal;
            RequirementsTissueSampleRemaining = requirementsTissueSampleRemaining;
            RequirementsTitanPodsTotal = requirementsTitanPodsTotal;
            RequirementsTitanPodsRemaining = requirementsTitanPodsRemaining;
        }
    }

    public class OverwatchStarSystemDetailProgress
    {
        public OverwatchThargoidLevel State { get; }
        public DateOnly Date { get; }
        public DateTimeOffset DateTime { get; }
        public int Progress { get; }
        public decimal ProgressPercentage { get; }
        public OverwatchStarSystemDetailProgress(StarSystemThargoidLevelProgress starSystemThargoidLevelProgress)
        {
            State = new(starSystemThargoidLevelProgress.ThargoidLevel);
            Date = DateOnly.FromDateTime(starSystemThargoidLevelProgress.Updated.DateTime);
            DateTime = starSystemThargoidLevelProgress.Updated;
            ProgressPercentage = starSystemThargoidLevelProgress.ProgressPercent ?? 0m;
            Progress = (int)Math.Floor(ProgressPercentage * 100);
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
                (starSystemThargoidLevel.CurrentProgress?.IsCompleted ?? false) &&
                (starSystemThargoidLevel.CycleEnd == null ||
                starSystemThargoidLevel.CycleEnd.Start >= new DateTimeOffset(2022, 12, 22, 7, 0, 0, TimeSpan.Zero)));
            AnalysisCycle = DateOnly.FromDateTime((starSystemThargoidLevel.CycleEnd?.Start ?? WeeklyTick.GetLastTick()).DateTime);
            ThargoidLevel = new(starSystemThargoidLevel);
            StateStart = starSystemThargoidLevel.CycleStart!.Start;
            StateEnds = starSystemThargoidLevel.CycleEnd?.End;
            StateIngameTimerExpires = starSystemThargoidLevel.StateExpires?.End;
            Progress = starSystemThargoidLevel.CurrentProgress?.ProgressLegacy;
            if (Progress != null && Progress > 100)
            {
                Progress = 100;
            }
            ProgressPercentage = starSystemThargoidLevel.CurrentProgress?.ProgressPercent;
            if (ProgressPercentage != null && ProgressPercentage > 1m)
            {
                ProgressPercentage = 1m;
            }
        }
    }
}
