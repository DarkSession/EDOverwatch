using EDDatabase;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace EDDataProcessor
{
    public static class WeeklyReset
    {
        public static async Task ProcessWeeklyReset(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<string> updateColumns = new()
            {
                nameof(StarSystemThargoidLevel.Progress),
                nameof(StarSystemThargoidLevel.CurrentProgress),
            };
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);

            List<StarSystem> starSystems = await dbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .Include(s => s.ThargoidLevel!.Maelstrom)
                .Where(s =>
                    s.WarRelevantSystem &&
                    s.ThargoidLevel != null &&
                    s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                    s.ThargoidLevel.CycleEnd == null &&
                    s.ThargoidLevel.CycleStart!.Start > currentThargoidCycle.Start &&
                    s.ThargoidLevel.Progress == 100)
                .ToListAsync(cancellationToken);

            foreach (StarSystem starSystem in starSystems)
            {
                StarSystemThargoidLevel oldThargoidLevel = starSystem.ThargoidLevel!;
                oldThargoidLevel.CycleEnd = currentThargoidCycle;

                StarSystemThargoidLevelState newState = oldThargoidLevel.State switch
                {
                    StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                    StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recapture,
                    StarSystemThargoidLevelState.Recovery => StarSystemThargoidLevelState.None,
                    StarSystemThargoidLevelState.Controlled => StarSystemThargoidLevelState.Recapture,
                    _ => StarSystemThargoidLevelState.None,
                };
                StarSystemThargoidLevel newThargoidCycle = new(0, newState, null, DateTimeOffset.UtcNow)
                {
                    StarSystem = starSystem,
                    CycleStart = currentThargoidCycle,
                    Maelstrom = oldThargoidLevel.Maelstrom,
                };
                dbContext.StarSystemThargoidLevels.Add(newThargoidCycle);
                starSystem.ThargoidLevel = newThargoidCycle;

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await dbContext.StarSystemThargoidLevels
                .Where(a => 
                    a.State > StarSystemThargoidLevelState.None && 
                    a.Progress != null &&
                    a.CycleEnd == null && 
                    a.CycleStart!.Start > currentThargoidCycle.Start)
                .BatchUpdateAsync(new StarSystemThargoidLevel(null, null), updateColumns, cancellationToken);

            await dbContext.BulkSaveChangesAsync(cancellationToken: cancellationToken);
        }
    }
}
