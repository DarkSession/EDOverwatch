using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [AllowAnonymous]
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
        public Task<OverwatchSystems> Systems(CancellationToken cancellationToken)
        {
            return OverwatchSystems.Create(DbContext, cancellationToken);
        }
    }
}
