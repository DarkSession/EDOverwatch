using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [AllowAnonymous]
    [EnableCors("ApiCORS")]
    public class Overwatch : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private IMemoryCache MemoryCache { get; }
        public Overwatch(EdDbContext dbContext, IMemoryCache memoryCache)
        {
            DbContext = dbContext;
            MemoryCache = memoryCache;
        }

        [HttpGet]
        public Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            return OverwatchOverview.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchOverviewV2> OverviewV2(CancellationToken cancellationToken)
        {
            return OverwatchOverviewV2.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchStarSystems> Systems(CancellationToken cancellationToken)
        {
            return OverwatchStarSystems.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet("{cycle}")]
        public Task<OverwatchStarSystemsHistorical> Systems(DateOnly cycle, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemsHistorical.Create(cycle, DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet("{systemAddress}")]
        public Task<OverwatchStarSystemFullDetail?> System(long systemAddress, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemFullDetail.Create(systemAddress, DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchMaelstroms> Maelstroms(CancellationToken cancellationToken) => Titans(cancellationToken);

        [HttpGet]
        public Task<OverwatchMaelstroms> Titans(CancellationToken cancellationToken)
        {
            return OverwatchMaelstroms.Create(DbContext, MemoryCache, cancellationToken);
        }

        private const string ThargoidCyclesCacheKey = "OverwatchThargoidCycles";
        [HttpGet]
        public async Task<List<OverwatchThargoidCycle>> ThargoidCycles(CancellationToken cancellationToken)
        {
            if (!MemoryCache.TryGetValue(ThargoidCyclesCacheKey, out List<OverwatchThargoidCycle>? result))
            {
                result = await OverwatchThargoidCycle.GetThargoidCycles(DbContext, cancellationToken);
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                MemoryCache.Set(ThargoidCyclesCacheKey, result, cacheEntryOptions);
            }
            return result!;
        }

        [HttpGet]
        public Task<OverwatchAlertPredictions> AlertPredictions(CancellationToken cancellationToken)
        {
            return Models.OverwatchAlertPredictions.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchWarStats> Stats(CancellationToken cancellationToken)
        {
            return OverwatchWarStats.Create(DbContext, MemoryCache, cancellationToken);
        }
    }
}
