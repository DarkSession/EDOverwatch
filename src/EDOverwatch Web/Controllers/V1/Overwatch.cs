using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [AllowAnonymous]
    [EnableCors("ApiCORS")]
    public class Overwatch : ControllerBase
    {
        private EdDbContext DbContext { get; }
        public Overwatch(EdDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [HttpGet]
        public Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            return OverwatchOverview.LoadOverwatchOverview(DbContext, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchStarSystems> Systems(CancellationToken cancellationToken)
        {
            return OverwatchStarSystems.Create(DbContext, cancellationToken);
        }

        [HttpGet("{cycle}")]
        public async Task<OverwatchStarSystemsHistorical> Systems(DateOnly cycle, CancellationToken cancellationToken)
        {
            if (cycle > DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date) || !await DbContext.ThargoidCycleExists(cycle, cancellationToken))
            {
                cycle = DateOnly.FromDateTime(WeeklyTick.GetTickTime(DateTimeOffset.UtcNow).DateTime);
            }
            return await OverwatchStarSystemsHistorical.Create(cycle, DbContext, cancellationToken);
        }

        [HttpGet("{systemAddress}")]
        public Task<OverwatchStarSystemDetail?> System(long systemAddress, CancellationToken cancellationToken)
        {
            return OverwatchStarSystemDetail.Create(systemAddress, DbContext, cancellationToken);
        }

        [HttpGet]
        public Task<OverwatchMaelstroms> Maelstroms(CancellationToken cancellationToken)
        {
            return OverwatchMaelstroms.Create(DbContext, cancellationToken);
        }

        [HttpGet]
        public Task<List<OverwatchThargoidCycle>> ThargoidCycles(CancellationToken cancellationToken)
        {
            return OverwatchThargoidCycle.GetThargoidCycles(DbContext, cancellationToken);
        }
    }
}
