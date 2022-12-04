﻿using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDOverwatch_Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class Overwatch : ControllerBase
    {
        private EdDbContext DbContext { get; }
        public Overwatch(EdDbContext dbContext)
        {
            DbContext = dbContext;
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
            int relevantSystemCount = await DbContext.StarSystems
                .Where(s => s.WarRelevantSystem)
                .CountAsync(cancellationToken);
            if (relevantSystemCount == 0)
            {
                relevantSystemCount = 1;
            }

            {
                int thargoidsSystemsControlling = await DbContext.StarSystems
                    .Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled)
                    .CountAsync(cancellationToken);
                result.Thargoids = new(
                    Math.Round((double)thargoidsSystemsControlling / (double)relevantSystemCount, 4),
                    await DbContext.ThargoidMaelstroms.CountAsync(cancellationToken),
                    thargoidsSystemsControlling
                );
            }
            {
                int humansSystemsControlling = await DbContext.StarSystems
                    .Where(s => s.WarRelevantSystem && (s.ThargoidLevel == null || s.ThargoidLevel!.State == StarSystemThargoidLevelState.None) && s.Population > 0)
                    .CountAsync(cancellationToken);
                result.Humans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0);
            }
            result.Contested = new(
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion).CountAsync(cancellationToken),
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert).CountAsync(cancellationToken),
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recapture).CountAsync(cancellationToken)
            );
            return result;
        }

        /*
        [HttpGet]
        public async Task<OverwatchSystems> Systems(CancellationToken cancellationToken)
        {
            List<StarSystem> starSystems = await DbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .Where(s => s.ThargoidLevel != null && s.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert)
                .ToListAsync(cancellationToken);
        }
        */
    }
}
