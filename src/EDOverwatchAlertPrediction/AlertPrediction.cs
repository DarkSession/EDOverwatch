using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDOverwatchAlertPrediction
{
    public static class AlertPrediction
    {
        public static readonly DateTimeOffset CycleZero = new(2022, 11, 24, 7, 0, 0, TimeSpan.Zero);

        public static async Task PredictionForCycle(EdDbContext dbContext, ThargoidCycle thargoidCycle, CancellationToken cancellationToken)
        {
            AlertPredictionInstance alertPredictionInstance = new(thargoidCycle);
            await alertPredictionInstance.PredictionForCycle(dbContext, cancellationToken);
        }

        public static async Task UpdateAttackersForCycle(EdDbContext dbContext, ThargoidCycle thargoidCycle, CancellationToken cancellationToken)
        {
            List<StarSystemThargoidLevel> starSystemThargoidLevels = await dbContext.StarSystemThargoidLevels
                .Where(s => ((s.State == StarSystemThargoidLevelState.Alert && s.StateExpires == thargoidCycle) || (s.State == StarSystemThargoidLevelState.Invasion && s.PreviousState != StarSystemThargoidLevelState.Alert)) && s.CycleEnd == null)
                .ToListAsync(cancellationToken);

            List<EDDatabase.AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .Include(a => a.Attackers!.Where(a => a.Status == AlertPredictionAttackerStatus.Default))
                .Where(a => a.Cycle == thargoidCycle)
                .OrderBy(a => a.Order)
                .ToListAsync(cancellationToken);

            List<AlertPredictionCycleAttacker> alertPredictionCycleAttackers = await dbContext.AlertPredictionCycleAttackers
                .Include(a => a.VictimStarSystem)
                .Where(a => a.Cycle == thargoidCycle)
                .ToListAsync(cancellationToken);

            List<StarSystem> usedAttackers = [];

            foreach (EDDatabase.AlertPrediction alertPrediction in alertPredictions)
            {
                if (starSystemThargoidLevels.Any(s => s.StarSystem == alertPrediction.StarSystem))
                {
                    StarSystem? attackingSystem = alertPrediction.Attackers?
                        .Where(a => a.Status == AlertPredictionAttackerStatus.Default)
                        .OrderBy(a => a.Order)
                        .Select(a => a.StarSystem)
                        .FirstOrDefault(s => !usedAttackers.Contains(s!));
                    if (attackingSystem != null)
                    {
                        usedAttackers.Add(attackingSystem);
                        AlertPredictionCycleAttacker? alertPredictionCycleAttacker = alertPredictionCycleAttackers.FirstOrDefault(a => a.VictimStarSystem == alertPrediction.StarSystem);
                        if (alertPredictionCycleAttacker != null)
                        {
                            alertPredictionCycleAttackers.Remove(alertPredictionCycleAttacker);
                            alertPredictionCycleAttacker.AttackerStarSystem = attackingSystem;
                        }
                        else
                        {
                            alertPredictionCycleAttacker = new(0)
                            {
                                Cycle = thargoidCycle,
                                AttackerStarSystem = attackingSystem,
                                VictimStarSystem = alertPrediction.StarSystem,
                            };
                            dbContext.AlertPredictionCycleAttackers.Add(alertPredictionCycleAttacker);
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
