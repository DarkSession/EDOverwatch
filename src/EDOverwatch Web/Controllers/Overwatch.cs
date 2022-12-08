using EDOverwatch_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDOverwatch_Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [AllowAnonymous]
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
                .Where(s =>
                    s.WarRelevantSystem &&
                    (s.Population > 0 ||
                        (s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled ||
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Maelstrom)))
                .CountAsync(cancellationToken);
            if (relevantSystemCount == 0)
            {
                relevantSystemCount = 1;
            }

            {
                int thargoidsSystemsControlling = await DbContext.StarSystems
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled ||
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Maelstrom)
                    .CountAsync(cancellationToken);
                var warEfforts = await DbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => w.Side == WarEffortSide.Thargoids)
                    .GroupBy(w => w.Type)
                    .Select(w => new
                    {
                        type = w.Key,
                        amount = w.Sum(s => s.Amount)
                    })
                    .ToListAsync(cancellationToken);
                result.Thargoids = new(
                    Math.Round((double)thargoidsSystemsControlling / (double)relevantSystemCount, 4),
                    await DbContext.ThargoidMaelstroms.CountAsync(cancellationToken),
                    thargoidsSystemsControlling,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.Kill)?.amount
                );
            }
            {
                int humansSystemsControlling = await DbContext.StarSystems
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Controlled &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Maelstrom &&
                        s.Population > 0)
                    .CountAsync(cancellationToken);

                var warEfforts = await DbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => w.Side == WarEffortSide.Humans)
                    .GroupBy(w => w.Type)
                    .Select(w => new
                    {
                        type = w.Key,
                        amount = w.Sum(s => s.Amount)
                    })
                    .ToListAsync(cancellationToken);

                result.Humans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.Kill)?.amount,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.Rescue)?.amount,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.SupplyDelivery)?.amount);
            }
            result.Contested = new(
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion).CountAsync(cancellationToken),
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert).CountAsync(cancellationToken),
                await DbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recapture).CountAsync(cancellationToken)
            );
            return result;
        }

        [HttpGet]
        public async Task<OverwatchSystems> Systems(CancellationToken cancellationToken)
        {
            List<StarSystem> starSystems = await DbContext.StarSystems
                .AsNoTracking()
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .Where(s =>
                            s.ThargoidLevel != null &&
                            s.ThargoidLevel.State >= StarSystemThargoidLevelState.Alert &&
                            s.ThargoidLevel.Maelstrom != null &&
                            s.ThargoidLevel.Maelstrom.StarSystem != null)
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await DbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken: cancellationToken);

            var efforts = await DbContext.WarEfforts
                .AsNoTracking()
                .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.Today))
                .GroupBy(w => new
                {
                    w.StarSystemId,
                    w.Type,
                })
                .Select(w => new
                {
                    w.Key.StarSystemId,
                    w.Key.Type,
                    Amount = w.Sum(g => g.Amount),
                })
                .ToListAsync(cancellationToken);

            Dictionary<WarEffortType, long> effortSums = new();
            foreach (var total in efforts.GroupBy(e => e.Type).Select(e => new
            {
                e.Key,
                Amount = e.Sum(g => g.Amount),
            }))
            {
                effortSums[total.Key] = total.Amount;
            }

            OverwatchSystems result = new(maelstroms);
            foreach (StarSystem starSystem in starSystems)
            {
                decimal effortFocus = 0;
                if (effortSums.Any())
                {
                    foreach (var effort in efforts
                        .Where(e => e.StarSystemId == starSystem.Id)
                        .GroupBy(e => e.Type)
                        .Select(e => new
                        {
                            e.Key,
                            Amount = e.Sum(g => g.Amount),
                        }))
                    {
                        if (effortSums.TryGetValue(effort.Key, out long totalAmount) && totalAmount != 0)
                        {
                            effortFocus += ((decimal)effort.Amount / (decimal)totalAmount);
                        }
                    }
                    effortFocus = Math.Round(effortFocus / (decimal)effortSums.Count, 2);
                }
                result.Systems.Add(new OverwatchStarSystem(starSystem, effortFocus));
            }
            return result;
        }
    }
}
