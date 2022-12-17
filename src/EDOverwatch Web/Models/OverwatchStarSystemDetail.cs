namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemDetail : OverwatchStarSystem
    {
        public long Population { get; }
        public List<OverwatchStarSystemWarEffort> WarEfforts { get; }
        public List<FactionOperation> FactionOperationDetails { get; }

        public OverwatchStarSystemDetail(StarSystem starSystem, decimal effortFocus, List<EDDatabase.WarEffort> warEfforts, List<FactionOperation> factionOperationDetails) :
            base(starSystem, effortFocus, 0)
        {
            Population = starSystem.Population;
            WarEfforts = warEfforts.Select(w => new OverwatchStarSystemWarEffort(w)).ToList();
            FactionOperations = factionOperationDetails.Count;
            FactionOperationDetails = factionOperationDetails;
        }

        public static async Task<OverwatchStarSystemDetail?> Create(long systemAddress, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            StarSystem? starSystem = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
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
                            if (systemEffortSums.TryGetValue(effort.Key, out long amount))
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
                        .Where(s => s.StarSystem == starSystem)
                        .ToListAsync(cancellationToken);
                    factionOperations = dcohFactionOperation.Select(d => new FactionOperation(d)).ToList();
                }

                return new OverwatchStarSystemDetail(starSystem, effortFocus, warEfforts, factionOperations);
            }
            return null;
        }
    }
}
