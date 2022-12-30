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
    }
}
