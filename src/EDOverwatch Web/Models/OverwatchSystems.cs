namespace EDOverwatch_Web.Models
{
    public class OverwatchSystems
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>().Select(s => new OverwatchThargoidLevel(s)).ToList();
        public List<OverwatchStarSystem> Systems { get; } = new();

        public OverwatchSystems(List<ThargoidMaelstrom> thargoidMaelstroms)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
        }

        public static async Task<OverwatchSystems> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<StarSystem> starSystems = await dbContext.StarSystems
               .AsNoTracking()
               .Include(s => s.ThargoidLevel)
               .ThenInclude(t => t!.Maelstrom)
               .ThenInclude(m => m!.StarSystem)
               .Where(s =>
                           s.ThargoidLevel != null &&
                           s.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert &&
                           s.ThargoidLevel.Maelstrom != null &&
                           s.ThargoidLevel.Maelstrom.StarSystem != null)
               .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            var efforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) && w.StarSystem!.WarRelevantSystem)
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

            Dictionary<WarEffortTypeGroup, long> totalEffortSums = new();
            foreach (var total in efforts.GroupBy(e => e.Type).Select(e => new
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

            OverwatchSystems result = new(maelstroms);
            foreach (StarSystem starSystem in starSystems)
            {
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
                        if (WarEffort.WarEffortGroups.TryGetValue(systemEfforts.Key, out WarEffortTypeGroup group))
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
                            if (totalEffortSums.TryGetValue(effort.Key, out long totalAmount))
                            {
                                effortFocus += ((decimal)effort.Value / (decimal)totalAmount / (decimal)totalEffortSums.Count);
                            }
                        }
                        effortFocus = Math.Round(effortFocus, 2);
                    }
                }
                result.Systems.Add(new OverwatchStarSystem(starSystem, effortFocus));
            }
            return result;
        }
    }
}
