using EDOverwatch_Web.Models;
using LazyCache;
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
        private IAppCache AppCache { get; }
        public Overwatch(EdDbContext dbContext, IAppCache appCache)
        {
            DbContext = dbContext;
            AppCache = appCache;
        }

        [Obsolete("Use Stats API")]
        [HttpGet]
        public Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            return OverwatchOverview.Create(DbContext, AppCache, cancellationToken);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of systems of the current cycle and some more", Description = "Returns a list of systems which are affected by the Thargoid war in the current cycle and some numbers about the previous, current and next cycle.")]
        public Task<OverwatchOverviewV2> OverviewV2(CancellationToken cancellationToken)
        {
            return OverwatchOverviewV2.Create(DbContext, AppCache, cancellationToken);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of systems of the current cycle")]
        public Task<OverwatchStarSystems> Systems(CancellationToken cancellationToken)
        {
            return OverwatchStarSystems.Create(DbContext, AppCache, cancellationToken);
        }

        [HttpGet("{cycle}")]
        [SwaggerOperation(Summary = "Returns a list of systems for a cycle")]
        public Task<OverwatchStarSystemsHistorical> Systems(DateOnly cycle, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemsHistorical.Create(cycle, DbContext, AppCache, cancellationToken);
        }

        [HttpGet("{systemAddress}")]
        [SwaggerOperation(Summary = "Returns detailed data for a system")]
        [SwaggerResponse(200, "System data")]
        [SwaggerResponse(204, "System not found or not relevant for the Thargoid war")]
        public Task<OverwatchStarSystemFullDetail?> System(long systemAddress, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemFullDetail.Create(systemAddress, DbContext, AppCache, cancellationToken);
        }

        [HttpGet]
        [Route("~/api/v1/[controller]/System/{systemAddress}/Progress")]
        [SwaggerOperation(Summary = "Returns a detailed progress history for a system for the current and the last tick.")]
        public Task<List<OverwatchStarSystemDetailProgress>?> SystemProgress(long systemAddress, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemDetailProgress.Create(systemAddress, DbContext, AppCache, cancellationToken);
        }

        [Obsolete("Use Titans API")]
        [HttpGet]
        public Task<OverwatchMaelstroms> Maelstroms(CancellationToken cancellationToken) => Titans(cancellationToken);

        [HttpGet]
        [SwaggerOperation(Summary = "Returns a list of all Titans")]
        public Task<OverwatchMaelstroms> Titans(CancellationToken cancellationToken)
        {
            return OverwatchMaelstroms.Create(DbContext, AppCache, cancellationToken);
        }

        private const string ThargoidCyclesCacheKey = "OverwatchThargoidCycles";
        [HttpGet]
        [SwaggerOperation(Summary = "Returns all past and the current weekly cycle.")]
        public Task<List<OverwatchThargoidCycle>> ThargoidCycles(CancellationToken cancellationToken)
        {
            return AppCache.GetOrAddAsync(ThargoidCyclesCacheKey, (cacheEntry) =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                return OverwatchThargoidCycle.GetThargoidCycles(DbContext, cancellationToken);
            });
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Returns the alert predictions for the next cycle.")]
        public Task<OverwatchAlertPredictions> AlertPredictions(CancellationToken cancellationToken)
        {
            return OverwatchAlertPredictions.Create(DbContext, AppCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchWarStats> Stats(CancellationToken cancellationToken)
        {
            return OverwatchWarStats.Create(DbContext, AppCache, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchSpireSites> SpireSites(CancellationToken cancellationToken)
        {
            return OverwatchSpireSites.Create(DbContext, AppCache, cancellationToken);
        }
    }
}
