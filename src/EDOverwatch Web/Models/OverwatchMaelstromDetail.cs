namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstromDetail : OverwatchMaelstrom
    {
        public List<OverwatchStarSystem> Systems { get; }
        public List<OverwatchMaelstromDetailSystemAtRisk> SystemsAtRisk { get; }

        protected OverwatchMaelstromDetail(ThargoidMaelstrom thargoidMaelstrom, List<OverwatchStarSystem> systems, List<OverwatchMaelstromDetailSystemAtRisk> systemAtRisks) :
            base(thargoidMaelstrom)
        {
            Systems = systems;
            SystemsAtRisk = systemAtRisks;
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
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 0);

            DateTimeOffset stationMaxAge = DateTimeOffset.UtcNow.AddDays(-1);
            if (currentThargoidCycle.Start < stationMaxAge)
            {
                stationMaxAge = currentThargoidCycle.Start;
            }

            var systems = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevel!.Maelstrom == maelstrom && s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Select(s => new
                {
                    StarSystem = s,
                    FactionOperations = s.FactionOperations!.Count(),
                    SpecialFactionOperations = s.FactionOperations!.Where(f => f.Faction!.SpecialFaction).Select(s => new
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

            var efforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) && systems.Select(y => y.StarSystem).Contains(w.StarSystem!))
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
                    Dictionary<WarEffortTypeGroup, long> systemEffortSums = new();
                    foreach (var systemEfforts in efforts
                        .Where(e => e.StarSystemId == starSystem.Id)
                        .GroupBy(e => e.Type)
                        .Select(e => new
                        {
                            e.Key,
                            Amount = e.Sum(g => g.Amount),
                        }))
                    {
                        if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(systemEfforts.Key, out WarEffortTypeGroup group))
                        {
                            if (!systemEffortSums.ContainsKey(group))
                            {
                                systemEffortSums[group] = systemEfforts.Amount;
                                continue;
                            }
                            systemEffortSums[group] += systemEfforts.Amount;
                        }
                    }
                    if (systemEffortSums.Any())
                    {
                        foreach (KeyValuePair<WarEffortTypeGroup, long> effort in systemEffortSums)
                        {
                            if (totalEffortSums.TryGetValue(effort.Key, out long totalAmount) && totalAmount > 0)
                            {
                                effortFocus += ((decimal)effort.Value / (decimal)totalAmount / (decimal)totalEffortSums.Count);
                            }
                        }
                        effortFocus = Math.Round(effortFocus, 2);
                    }
                }
                List<OverwatchStarSystemSpecialFactionOperation> specialFactionOperations = system.SpecialFactionOperations
                    .Select(s => new OverwatchStarSystemSpecialFactionOperation(s.Short, s.Name))
                    .ToList();
                resultStarSystems.Add(new OverwatchStarSystem(starSystem, effortFocus, system.FactionOperations, specialFactionOperations, system.StationsUnderRepair, system.StationsDamaged, system.StationsUnderAttack));
            }

            List<OverwatchMaelstromDetailSystemAtRisk> systemsAtRiskResult = new();
            decimal systemsAtRiskSphere = maelstrom.InfluenceSphere + 1m;
            List<StarSystem> systemsAtRisk = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s =>
                    s.LocationX >= maelstrom.StarSystem.LocationX - systemsAtRiskSphere && s.LocationX <= maelstrom.StarSystem.LocationX + systemsAtRiskSphere &&
                    s.LocationY >= maelstrom.StarSystem.LocationY - systemsAtRiskSphere && s.LocationY <= maelstrom.StarSystem.LocationY + systemsAtRiskSphere &&
                    s.LocationZ >= maelstrom.StarSystem.LocationZ - systemsAtRiskSphere && s.LocationZ <= maelstrom.StarSystem.LocationZ + systemsAtRiskSphere &&
                    s.Population > 0 &&
                    (s.ThargoidLevel == null || s.ThargoidLevel.State == StarSystemThargoidLevelState.None))
                .ToListAsync(cancellationToken);
            foreach (StarSystem systemAtRisk in systemsAtRisk)
            {
                double distance = Math.Round(maelstrom.StarSystem.DistanceTo(systemAtRisk), 2);
                if (distance > (double)systemsAtRiskSphere)
                {
                    continue;
                }
                systemsAtRiskResult.Add(new OverwatchMaelstromDetailSystemAtRisk(systemAtRisk.Name, distance, systemAtRisk.Population));
            }

            return new OverwatchMaelstromDetail(maelstrom, resultStarSystems, systemsAtRiskResult);
        }
    }
}
