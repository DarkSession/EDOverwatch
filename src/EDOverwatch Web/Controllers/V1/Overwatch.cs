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
                .Where(w => w.Date >= DateOnly.FromDateTime(DateTime.UtcNow))
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
