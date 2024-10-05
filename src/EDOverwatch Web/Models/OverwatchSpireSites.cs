using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Models
{
    public class OverwatchSpireSites
    {
        public List<OverwatchStarSystem> Systems { get; }

        internal OverwatchSpireSites(List<OverwatchStarSystem> systems)
        {
            Systems = systems;
        }

        private const string CacheKey = "OverwatchSpireSites";

        public static void DeleteMemoryEntry(IAppCache appCache)
        {
            appCache.Remove(CacheKey);
        }

        public static Task<OverwatchSpireSites> Create(EdDbContext dbContext, IAppCache appCache, CancellationToken cancellationToken)
        {
            return appCache.GetOrAddAsync(CacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
                return CreateInternal(dbContext, cancellationToken);
            })!;
        }

        private static async Task<OverwatchSpireSites> CreateInternal(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            var starSystems = await dbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .Where(s => s.BarnacleMatrixInSystem && s.ThargoidLevel!.Maelstrom!.State == ThargoidMaelstromState.Active)
                .ToListAsync(cancellationToken);

            var systems = starSystems.Select(s => new OverwatchStarSystem(s, false)).ToList();

            return new OverwatchSpireSites(systems);
        }
    }
}
