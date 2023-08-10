using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDOverwatchAlertPrediction
{
    public static class AlertPrediction
    {
        public static readonly DateTimeOffset CycleZero = new(2022, 11, 24, 7, 0, 0, TimeSpan.Zero);

        public static async Task PredictionForCycle(EdDbContext dbContext, ThargoidCycle thargoidCycle, CancellationToken cancellationToken)
        {
            int cycle = (int)(thargoidCycle.Start - CycleZero).TotalDays / 7;

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            List<StarSystemCycleState> starSystems;
            {
                List<StarSystem> dbStarSystems = await dbContext.StarSystems
                    .AsNoTracking()
                    .Include(s => s.ThargoidLevelHistory!)
                    .ThenInclude(t => t.Maelstrom)
                    .Include(s => s.ThargoidLevelHistory!)
                    .ThenInclude(t => t.CycleStart)
                    .Include(s => s.ThargoidLevelHistory!)
                    .ThenInclude(t => t.CycleEnd)
                    .Include(s => s.ThargoidLevelHistory!)
                    .ThenInclude(t => t.StateExpires)
                    .Where(s => s.WarRelevantSystem)
                    .ToListAsync(cancellationToken);
                starSystems = dbStarSystems.Select(s => new StarSystemCycleState(s, cycle)).ToList();

                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(thargoidCycle.Start, cancellationToken, -1);

                List<AlertPredictionCycleAttacker> alertPredictionCycleAttackers = await dbContext.AlertPredictionCycleAttackers
                    .AsNoTracking()
                    .Where(a => a.Cycle == previousThargoidCycle)
                    .ToListAsync(cancellationToken);

                foreach (AlertPredictionCycleAttacker alertPredictionCycleAttacker in alertPredictionCycleAttackers)
                {
                    StarSystemCycleState? previousCycleAttackerSystem = starSystems.FirstOrDefault(s => s.SystemAddress == alertPredictionCycleAttacker.AttackerStarSystem?.SystemAddress);
                    if (previousCycleAttackerSystem == null)
                    {
                        continue;
                    }
                    previousCycleAttackerSystem.LastAttackCycle = cycle - 1;
                }
            }

            List<StarSystemCycleState> possibleAttackers = starSystems.Where(s => s.CanAttack()).ToList();
            List<StarSystemCycleState> possibleVictims = starSystems.Where(s => s.CanBeAttacked()).ToList();

            List<EDDatabase.AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .AsSplitQuery()
                .Include(a => a.Attackers!)
                .Where(a => a.Cycle == thargoidCycle)
                .ToListAsync(cancellationToken);
            List<EDDatabase.AlertPrediction> usedAlertPredictions = new();

            foreach (ThargoidMaelstrom maelstrom in maelstroms)
            {
                List<Attack> possibleAttacks = new();

                foreach (StarSystemCycleState attackingSystem in possibleAttackers.Where(p => p.Maelstrom == maelstrom.Name))
                {
                    foreach (StarSystemCycleState victimSystem in possibleVictims.Where(p => attackingSystem.CanAttackSystem(p)))
                    {
                        Attack? possibleAttack = possibleAttacks.FirstOrDefault(p => p.VictimSystem.SystemAddress == victimSystem.SystemAddress);
                        if (possibleAttack == null)
                        {
                            possibleAttack = new(victimSystem);
                            possibleAttacks.Add(possibleAttack);
                        }
                        possibleAttack.Attackers.Add(attackingSystem);
                    }
                }

                List<StarSystemCycleState> primaryAttackerSystems = new();
                int attackingCredits = 20;
                foreach (Attack attack in possibleAttacks.OrderBy(p => p.VictimSystem.DistanceTo(maelstrom.StarSystem!)))
                {
                    int attackCost = attack.VictimSystem.AttackCost();
                    bool alertLikely = attackingCredits >= attackCost;
                    if (alertLikely && !attack.Attackers!.Any(a => !primaryAttackerSystems.Contains(a)))
                    {
                        alertLikely = false;
                    }
                    if (alertLikely)
                    {
                        attackingCredits -= attackCost;
                    }

                    EDDatabase.AlertPrediction? systemAlertPrediction = alertPredictions.FirstOrDefault(a => a.StarSystemId == attack.VictimSystem.Id);
                    if (systemAlertPrediction == null)
                    {
                        systemAlertPrediction = new(0, attack.VictimSystem.Id, alertLikely, AlertPredictionStatus.Default)
                        {
                            Cycle = thargoidCycle,
                            Maelstrom = maelstrom,
                            Attackers = new(),
                        };
                        dbContext.AlertPredictions.Add(systemAlertPrediction);
                    }
                    systemAlertPrediction.AlertLikely = alertLikely;
                    usedAlertPredictions.Add(systemAlertPrediction);

                    List<AlertPredictionAttacker> usedAttackers = new();
                    List<StarSystemCycleState> attackers = attack.Attackers
                        .OrderBy(a => primaryAttackerSystems.Contains(a))
                        .ThenBy(a => a.DistanceTo(maelstrom.StarSystem!))
                        .ToList();
                    int order = 0;
                    foreach (StarSystemCycleState attacker in attackers)
                    {
                        if (order == 0)
                        {
                            primaryAttackerSystems.Add(attacker);
                        }
                        AlertPredictionAttacker? alertPredictionAttacker = systemAlertPrediction.Attackers!.FirstOrDefault(a => a.StarSystemId == attacker.Id);
                        if (alertPredictionAttacker == null)
                        {
                            alertPredictionAttacker = new(0, attacker.Id, order, AlertPredictionAttackerStatus.Default);
                            systemAlertPrediction.Attackers!.Add(alertPredictionAttacker);
                        }
                        alertPredictionAttacker.Order = order;
                        usedAttackers.Add(alertPredictionAttacker);
                        order++;
                    }

                    systemAlertPrediction.Attackers!.RemoveAll(a => !usedAttackers.Contains(a));
                }
            }

            dbContext.AlertPredictions.RemoveRange(alertPredictions.Where(a => !usedAlertPredictions.Contains(a)));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public static async Task UpdateAttackersForCycle(EdDbContext dbContext, ThargoidCycle thargoidCycle, CancellationToken cancellationToken)
        {
            List<StarSystemThargoidLevel> starSystemThargoidLevels = await dbContext.StarSystemThargoidLevels
                .Where(s => s.State == StarSystemThargoidLevelState.Alert && s.CycleEnd == null && s.StateExpires == thargoidCycle)
                .ToListAsync(cancellationToken);

            List<EDDatabase.AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .Include(a => a.Attackers)
                .Where(a => a.Cycle == thargoidCycle)
                .ToListAsync(cancellationToken);

            List<AlertPredictionCycleAttacker> alertPredictionCycleAttackers = await dbContext.AlertPredictionCycleAttackers
                .Include(a => a.VictimStarSystem)
                .Where(a => a.Cycle == thargoidCycle)
                .ToListAsync(cancellationToken);

            List<StarSystem> usedAttackers = new();

            foreach (EDDatabase.AlertPrediction alertPrediction in alertPredictions)
            {
                if (starSystemThargoidLevels.Any(s => s.StarSystem == alertPrediction.StarSystem))
                {
                    StarSystem? attackingSystem = alertPrediction.Attackers?
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
