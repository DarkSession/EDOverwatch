using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictions
    {
        public OverwatchThargoidCycle Cycle { get; }
        public List<OverwatchAlertPredictionMaelstrom> Maelstroms { get; set; }
        public List<OverwatchStarSystemMin> CurrentCycleAttackers { get; }

        public OverwatchAlertPredictions(ThargoidCycle thargoidCycle, List<ThargoidMaelstrom> maelstroms, List<AlertPrediction> alertPredictions, List<AlertPredictionCycleAttacker> alertPredictionCycleAttackers)
        {
            Cycle = new(thargoidCycle);
            Maelstroms = new();
            foreach (ThargoidMaelstrom maelstrom in maelstroms)
            {
                List<AlertPrediction> systems = alertPredictions
                    .Where(a => a.Maelstrom?.Id == maelstrom.Id)
                    .ToList();
                OverwatchAlertPredictionMaelstrom maelstromEntry = new(maelstrom, systems);
                Maelstroms.Add(maelstromEntry);
            }
            CurrentCycleAttackers = alertPredictionCycleAttackers.Select(a => new OverwatchStarSystemMin(a.AttackerStarSystem!)).ToList();
        }

        private const string CacheKey = "OverwatchAlertPredictions";

        public static void DeleteMemoryEntry(IAppCache appCache)
        {
            appCache.Remove(CacheKey);
        }

        public static Task<OverwatchAlertPredictions> Create(EdDbContext dbContext, IAppCache appCache, CancellationToken cancellationToken)
        {
            return appCache.GetOrAddAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchAlertPredictions> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle nextThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            List<AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Attackers!)
                .ThenInclude(a => a.StarSystem!.ThargoidLevel)
                .Where(a => a.Cycle == nextThargoidCycle)
                .ToListAsync(cancellationToken);

            List<AlertPredictionCycleAttacker> alertPredictionCycleAttackers = await dbContext.AlertPredictionCycleAttackers
                .AsNoTracking()
                .Where(a => a.Cycle == currentThargoidCycle)
                .ToListAsync(cancellationToken);

            return new OverwatchAlertPredictions(nextThargoidCycle, maelstroms, alertPredictions, alertPredictionCycleAttackers);
        }
    }
}
