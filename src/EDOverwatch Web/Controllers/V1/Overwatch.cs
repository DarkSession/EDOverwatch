using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.Annotations;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [Produces("application/json")]
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

        [Obsolete("Use Stats API")]
        [HttpGet]
        public Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            return OverwatchOverview.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of systems of the current cycle and some more", Description = "Returns a list of systems which are affected by the Thargoid war in the current cycle and some numbers about the previous, current and next cycle.")]
        public Task<OverwatchOverviewV2> OverviewV2(CancellationToken cancellationToken)
        {
            return OverwatchOverviewV2.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of systems of the current cycle")]
        public Task<OverwatchStarSystems> Systems(CancellationToken cancellationToken)
        {
            return OverwatchStarSystems.Create(DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet("{cycle}")]
        [SwaggerOperation(Summary = "Returns a list of systems for a cycle")]
        public Task<OverwatchStarSystemsHistorical> Systems(DateOnly cycle, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemsHistorical.Create(cycle, DbContext, MemoryCache, cancellationToken);
        }

        [HttpGet("{systemAddress}")]
        [SwaggerOperation(Summary = "Returns additional data for a system")]
        [SwaggerResponse(200, "System data")]
        [SwaggerResponse(204, "System not found or not relevant for the Thargoid war")]
        public Task<OverwatchStarSystemFullDetail?> System(long systemAddress, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemFullDetail.Create(systemAddress, DbContext, MemoryCache, cancellationToken);
        }

        [Obsolete("Use Titans API")]
        [HttpGet]
        public Task<OverwatchMaelstroms> Maelstroms(CancellationToken cancellationToken) => Titans(cancellationToken);

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of all Titans")]
        public Task<OverwatchMaelstroms> Titans(CancellationToken cancellationToken)
        {
            return OverwatchMaelstroms.Create(DbContext, MemoryCache, cancellationToken);
        }

        private const string ThargoidCyclesCacheKey = "OverwatchThargoidCycles";
        [HttpGet]
        [SwaggerOperation(Summary = "Returns all past and the current weekly cycle.")]
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
        [SwaggerOperation(Summary = "Returns the alert predictions for the next cycle.")]
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
