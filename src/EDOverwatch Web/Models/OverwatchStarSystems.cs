namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystems
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>().Select(s => new OverwatchThargoidLevel(s)).ToList();
        public List<OverwatchStarSystem> Systems { get; } = new();

        public OverwatchStarSystems(List<ThargoidMaelstrom> thargoidMaelstroms)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
        }

        public static async Task<OverwatchStarSystems> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            var systems = await dbContext.StarSystems
               .AsNoTracking()
               .Where(s =>
                           s.ThargoidLevel != null &&
                           s.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert &&
                           s.ThargoidLevel.Maelstrom != null &&
                           s.ThargoidLevel.Maelstrom.StarSystem != null)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Select(s => new
                {
                    StarSystem = s,
                    FactionOperations = s.FactionOperations!.Count(),
                })
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            Dictionary<WarEffortTypeGroup, long> totalEffortSums = await WarEffort.GetTotalWarEfforts(dbContext, cancellationToken);

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

            OverwatchStarSystems result = new(maelstroms);
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
                            if (totalEffortSums.TryGetValue(effort.Key, out long totalAmount))
                            {
                                effortFocus += ((decimal)effort.Value / (decimal)totalAmount / (decimal)totalEffortSums.Count);
                            }
                        }
                        effortFocus = Math.Round(effortFocus, 2);
                    }
                }
                result.Systems.Add(new OverwatchStarSystem(starSystem, effortFocus, system.FactionOperations));
            }
            return result;
        }
    }
}
