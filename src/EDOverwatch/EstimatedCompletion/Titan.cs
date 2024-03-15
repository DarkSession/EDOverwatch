using Microsoft.EntityFrameworkCore;

namespace EDOverwatch.EstimatedCompletion
{
    internal class Titan
    {
        internal static async Task UpdateTitanEstimatedCompletion(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidMaelstrom> titans = await dbContext.ThargoidMaelstroms
                .Include(m => m.StarSystem)
                .ThenInclude(s => s!.ThargoidLevel)
                .ThenInclude(t => t!.CurrentProgress)
                .Where(t => t.HeartsRemaining > 0)
                .ToListAsync(cancellationToken);

            foreach (ThargoidMaelstrom titan in titans)
            {
                if (titan.StarSystem?.ThargoidLevel is null)
                {
                    continue;
                }
                decimal currentProgress = titan.StarSystem.ThargoidLevel.CurrentProgress?.ProgressPercent ?? 0m;
                decimal remainingProgress = titan.HeartsRemaining - currentProgress;
                if (remainingProgress <= 0)
                {
                    continue;
                }

                decimal pastDayProgress = 0m;
                decimal pastHourProgress = 0;

                {
                    DateTimeOffset previousDayTime = DateTimeOffset.UtcNow.AddDays(-1);
                    var previousDayProgress = await dbContext.StarSystemThargoidLevelProgress
                        .AsNoTracking()
                        .Where(s => s.ThargoidLevel == titan.StarSystem.ThargoidLevel && s.Updated >= previousDayTime)
                        .OrderBy(s => s.Updated)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (previousDayProgress is not null)
                    {
                        pastDayProgress = currentProgress - (previousDayProgress.ProgressPercent ?? 0m);
                    }
                    pastDayProgress += await dbContext.ThargoidMaelstromHearts
                        .AsNoTracking()
                        .Where(s => s.Maelstrom == titan && s.DestructionTime >= previousDayTime)
                        .CountAsync(cancellationToken);
                }
                {
                    DateTimeOffset previousHourTime = DateTimeOffset.UtcNow.AddHours(-1);
                    var previousHourProgress = await dbContext.StarSystemThargoidLevelProgress
                        .AsNoTracking()
                        .Where(s => s.ThargoidLevel == titan.StarSystem.ThargoidLevel && s.Updated >= previousHourTime)
                        .OrderBy(s => s.Updated)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (previousHourProgress is not null)
                    {
                        pastHourProgress = currentProgress - (previousHourProgress.ProgressPercent ?? 0m);
                    }
                    pastHourProgress += await dbContext.ThargoidMaelstromHearts
                        .AsNoTracking()
                        .Where(s => s.Maelstrom == titan && s.DestructionTime >= previousHourTime)
                        .CountAsync(cancellationToken);
                }

                decimal predictedDailyProgress = pastDayProgress / 2m + pastHourProgress * 12;
                if (predictedDailyProgress <= 0)
                {
                    titan.CompletionTimeEstimate = null;
                    continue;
                }
                decimal daysRemaining = remainingProgress / predictedDailyProgress;
                if (daysRemaining > 365)
                {
                    titan.CompletionTimeEstimate = null;
                }
                else
                {
                    titan.CompletionTimeEstimate = DateTimeOffset.UtcNow.AddDays((double)Math.Round(daysRemaining, 4));
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
