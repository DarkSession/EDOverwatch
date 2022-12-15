namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemDetail : OverwatchStarSystem
    {
        public long Population { get; }
        public List<OverwatchStarSystemWarEffort> WarEfforts { get; }

        public OverwatchStarSystemDetail(StarSystem starSystem, decimal effortFocus, List<WarEffort> warEfforts) : base(starSystem, effortFocus)
        {
            Population = starSystem.Population;
            WarEfforts = warEfforts.Select(w => new OverwatchStarSystemWarEffort(w)).ToList();
        }

        public static async Task<OverwatchStarSystemDetail?> Create(long systemAddress, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            StarSystem? starSystem = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .FirstOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem?.ThargoidLevel != null)
            {
                var totalEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) && w.StarSystem!.WarRelevantSystem)
                    .GroupBy(w => new
                    {
                        w.Type,
                    })
                    .Select(w => new
                    {
                        w.Key.Type,
                        Amount = w.Sum(g => g.Amount),
                    })
                    .ToListAsync(cancellationToken);

                Dictionary<WarEffortTypeGroup, long> totalEffortSums = new();
                foreach (var total in totalEfforts.GroupBy(e => e.Type).Select(e => new
                {
                    e.Key,
                    Amount = e.Sum(g => g.Amount),
                }))
                {
                    if (WarEffort.WarEffortGroups.TryGetValue(total.Key, out WarEffortTypeGroup group))
                    {
                        if (!totalEffortSums.ContainsKey(group))
                        {
                            totalEffortSums[group] = total.Amount;
                            continue;
                        }
                        totalEffortSums[group] += total.Amount;
                    }
                }

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
                        if (WarEffort.WarEffortGroups.TryGetValue(systemEffort.Key, out WarEffortTypeGroup group))
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

                List<WarEffort> warEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)) && w.StarSystem == starSystem)
                    .OrderByDescending(w => w.Date)
                    .ToListAsync(cancellationToken);

                return new OverwatchStarSystemDetail(starSystem, effortFocus, warEfforts);
            }
            return null;
        }
    }
}
