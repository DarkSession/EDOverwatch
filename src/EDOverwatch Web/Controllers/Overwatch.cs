using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDOverwatch_Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class Overwatch : ControllerBase
    {
        private EdDbContext EdDbContext { get; }
        public Overwatch(EdDbContext dbContext)
        {
            EdDbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Health()
        {
            return Ok();
        }

        [HttpGet]
        public async Task<OverwatchOverview> Overview(CancellationToken cancellationToken)
        {
            OverwatchOverview result = new();
            int relevantSystemCount = await EdDbContext.StarSystems
                .Where(s => s.WarRelevantSystem)
                .CountAsync(cancellationToken);
            if (relevantSystemCount == 0)
            {
                relevantSystemCount = 1;
            }

            {
                int thargoidsSystemsControlling = await EdDbContext.StarSystems
                    .Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled)
                    .CountAsync(cancellationToken);
                result.Thargoids = new(
                    Math.Round((double)thargoidsSystemsControlling / (double)relevantSystemCount, 4),
                    await EdDbContext.ThargoidMaelstroms.CountAsync(cancellationToken),
                    thargoidsSystemsControlling
                );
            }
            {
                int humansSystemsControlling = await EdDbContext.StarSystems
                    .Where(s => s.WarRelevantSystem && (s.ThargoidLevel == null || s.ThargoidLevel!.State == StarSystemThargoidLevelState.None) && s.Population > 0)
                    .CountAsync(cancellationToken);
                result.Humans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0);
            }
            result.Contested = new(
                await EdDbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion).CountAsync(cancellationToken),
                await EdDbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert).CountAsync(cancellationToken),
                await EdDbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recapture).CountAsync(cancellationToken)
            );
            return result;
        }
    }
}
