using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstroms
    {
        public List<OverwatchMaelstromBasic> Maelstroms { get; }

        public OverwatchMaelstroms(List<OverwatchMaelstromBasic> maelstroms)
        {
            Maelstroms = maelstroms;
        }

        private const string CacheKey = "OverwatchMaelstroms";

        public static void DeleteMemoryEntry(IAppCache appCache)
        {
            appCache.Remove(CacheKey);
        }

        public static Task<OverwatchMaelstroms> Create(EdDbContext dbContext, IAppCache appCache, CancellationToken cancellationToken)
        {
            return appCache.GetOrAddAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchMaelstroms> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            var maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .Select(t => new
                {
                    Maelstrom = t,
                    SystemsInAlert = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert),
                    SystemsInInvasion = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion),
                    SystemsThargoidControlled = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled),
                    SystemsInRecovery = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recovery),
                    PopulatedSystemsInvaded = dbContext.StarSystemThargoidLevels.Count(s => s.Maelstrom == t && s.CycleEnd != null && s.CycleStart!.Start <= s.CycleEnd.Start && s.StarSystem!.OriginalPopulation > 0 && s.State == StarSystemThargoidLevelState.Invasion),
                    PopulatedAlertsDefended = dbContext.StarSystemThargoidLevels.Count(s => s.Maelstrom == t && s.CycleEnd != null && s.CycleStart!.Start <= s.CycleEnd.Start && s.StarSystem!.OriginalPopulation > 0 && s.State == StarSystemThargoidLevelState.Alert && s.CurrentProgress!.IsCompleted),
                    PopulatedInvasionsDefended = dbContext.StarSystemThargoidLevels.Count(s => s.Maelstrom == t && s.CycleEnd != null && s.CycleStart!.Start <= s.CycleEnd.Start && s.StarSystem!.OriginalPopulation > 0 && s.State == StarSystemThargoidLevelState.Invasion && s.CurrentProgress!.IsCompleted),
                })
                .ToListAsync(cancellationToken);
            List<OverwatchMaelstromBasic> result = maelstroms
                .Select(m =>
                    new OverwatchMaelstromBasic(m.Maelstrom, m.SystemsInAlert, m.SystemsInInvasion, m.SystemsThargoidControlled, m.SystemsInRecovery, m.PopulatedSystemsInvaded, m.PopulatedAlertsDefended, m.PopulatedInvasionsDefended))
                .ToList();

            return new OverwatchMaelstroms(result);
        }
    }
}
