using EDUtils;

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
                .ToListAsync(cancellationToken: cancellationToken);

            var efforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow))
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

            Dictionary<WarEffortType, long> effortSums = new();
            foreach (var total in efforts.GroupBy(e => e.Type).Select(e => new
            {
                e.Key,
                Amount = e.Sum(g => g.Amount),
            }))
            {
                effortSums[total.Key] = total.Amount;
            }

            OverwatchSystems result = new(maelstroms);
            foreach (StarSystem starSystem in starSystems)
            {
                decimal effortFocus = 0;
                if (effortSums.Any())
                {
                    foreach (var effort in efforts
                        .Where(e => e.StarSystemId == starSystem.Id)
                        .GroupBy(e => e.Type)
                        .Select(e => new
                        {
                            e.Key,
                            Amount = e.Sum(g => g.Amount),
                        }))
                    {
                        if (effortSums.TryGetValue(effort.Key, out long totalAmount) && totalAmount != 0)
                        {
                            effortFocus += ((decimal)effort.Amount / (decimal)totalAmount);
                        }
                    }
                    effortFocus = Math.Round(effortFocus / (decimal)effortSums.Count, 2);
                }
                result.Systems.Add(new OverwatchStarSystem(starSystem, effortFocus));
            }
            return result;
        }
    }

    public class OverwatchMaelstrom
    {
        public string Name { get; }
        public string SystemName { get; }

        public OverwatchMaelstrom(ThargoidMaelstrom thargoidMaelstrom)
        {
            Name = thargoidMaelstrom.Name;
            SystemName = thargoidMaelstrom.StarSystem?.Name ?? string.Empty;
        }
    }

    public class OverwatchThargoidLevel
    {
        public StarSystemThargoidLevelState Level { get; }
        public string Name { get; }

        public OverwatchThargoidLevel(StarSystemThargoidLevelState level)
        {
            Level = level;
            Name = level.GetEnumMemberValue();
        }
    }

    public class OverwatchStarSystem
    {
        public string Name { get; }
        public OverwatchMaelstrom Maelstrom { get; }
        public OverwatchThargoidLevel ThargoidLevel { get; }
        public short? Progress { get; }
        public decimal EffortFocus { get; }

        public OverwatchStarSystem(StarSystem starSystem, decimal effortFocus)
        {
            Name = starSystem.Name;
            Maelstrom = new(starSystem.ThargoidLevel?.Maelstrom ?? throw new Exception("Thargoid level must have a maelstrom property"));
            ThargoidLevel = new(starSystem.ThargoidLevel?.State ?? StarSystemThargoidLevelState.None);
            Progress = starSystem.ThargoidLevel?.Progress;
            EffortFocus = effortFocus;
        }
    }
}
