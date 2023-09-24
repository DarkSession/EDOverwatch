using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDOverwatchAlertPrediction
{
    internal class AlertPredictionInstance
    {
        private int Cycle { get; }
        private ThargoidCycle ThargoidCycle { get; }
        private List<StarSystemCycleState> PrimaryAttackerSystems { get; } = new();
        private List<EDDatabase.AlertPrediction> UsedAlertPredictions { get; } = new();

        enum AttackMode : byte
        {
            Closest,
            NearestAttacksFurthest,
        }

        public AlertPredictionInstance(ThargoidCycle thargoidCycle)
        {
            ThargoidCycle = thargoidCycle;
            Cycle = (int)(thargoidCycle.Start - AlertPrediction.CycleZero).TotalDays / 7;
        }

        public async Task PredictionForCycle(EdDbContext dbContext, CancellationToken cancellationToken)
        {
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
                starSystems = dbStarSystems.Select(s => new StarSystemCycleState(s, Cycle)).ToList();

                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(ThargoidCycle.Start, cancellationToken, -1);

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
                    previousCycleAttackerSystem.LastAttackCycle = Cycle - 1;
                }
            }

            List<StarSystemCycleState> possibleAttackers = starSystems.Where(s => s.CanAttack()).ToList();
            List<StarSystemCycleState> possibleVictims = starSystems.Where(s => s.CanBeAttacked()).ToList();

            List<EDDatabase.AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .AsSplitQuery()
                .Include(a => a.Attackers!)
                .Where(a => a.Cycle == ThargoidCycle)
                .ToListAsync(cancellationToken);

            foreach (ThargoidMaelstrom maelstrom in maelstroms)
            {
                List<Attack> possibleAttacks = new();
                int maelstromAlertCount = 0;

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

                var x = starSystems
                    .Where(s => s.Maelstrom == maelstrom.Name && s.ThargoidLevel?.State == StarSystemThargoidLevelState.Controlled && !s.ThargoidLevel.Completed && !s.IsNewState)
                    .Select(s => new
                    {
                        distance = s.DistanceTo(maelstrom.StarSystem!),
                        system = s,
                    })
                    .ToList();

                int attackingCredits = 20;
                AttackMode attackMode = AttackMode.Closest;
                float maxControlSystemDistance = starSystems
                    .Where(s => s.Maelstrom == maelstrom.Name && s.ThargoidLevel?.State == StarSystemThargoidLevelState.Controlled && !s.ThargoidLevel.Completed && !s.IsNewState)
                    .Select(s => s.DistanceTo(maelstrom.StarSystem!))
                    .DefaultIfEmpty()
                    .Max();

                {
                    List<Attack> attackList = possibleAttacks.OrderBy(p => p.VictimSystem.DistanceTo(maelstrom.StarSystem!)).ToList();

                    foreach (Attack attack in attackList)
                    {
                        int attackCost = attack.VictimSystem.AttackCost();
                        bool alertPossible = attackingCredits >= attackCost;

                        StarSystemCycleState? primaryAttacker = attack.Attackers
                            .Where(a => !PrimaryAttackerSystems.Contains(a))
                            .OrderBy(a => a.DistanceTo(maelstrom.StarSystem!))
                            .FirstOrDefault();

                        if (alertPossible && primaryAttacker == null)
                        {
                            alertPossible = false;
                        }
                        if (alertPossible && attack.VictimSystem.DistanceTo(maelstrom.StarSystem!) > maxControlSystemDistance)
                        {
                            attackMode = AttackMode.NearestAttacksFurthest;
                            break;
                        }
                        if (alertPossible)
                        {
                            attackingCredits -= attackCost;
                            maelstromAlertCount++;
                        }

                        await ProcessAttack(attack, alertPossible, maelstrom, primaryAttacker, dbContext, cancellationToken);
                        possibleAttacks.Remove(attack);
                    }
                }

                if (attackMode == AttackMode.NearestAttacksFurthest && possibleAttacks.Any())
                {
                    IEnumerable<StarSystemCycleState> remainingAttackers = possibleAttacks
                        .SelectMany(p => p.Attackers)
                        .DistinctBy(a => a.SystemAddress)
                        .Where(p => !PrimaryAttackerSystems.Contains(p))
                        .OrderBy(a => a.DistanceTo(maelstrom.StarSystem!));
                    List<long> skippedAttacks = new();
                    int backtracks = 0;
                    foreach (StarSystemCycleState remainingAttacker in remainingAttackers)
                    {
                        IEnumerable<Attack> attackerAttacks = possibleAttacks
                            .Where(p => p.Attackers.Any(a => a.SystemAddress == remainingAttacker.SystemAddress))
                            .OrderByDescending(p => p.VictimSystem.WasPopulated)
                            .ThenByDescending(p => p.VictimSystem.DistanceTo(maelstrom.StarSystem!));
                        Attack? attack = attackerAttacks
                            .FirstOrDefault();
                        if (attack == null)
                        {
                            continue;
                        }

                        int attackCost = attack.VictimSystem.AttackCost();
                        bool alertPossible = attackingCredits >= attackCost;
                        float victimSystemDistanceToMaelstrom = attack.VictimSystem.DistanceTo(maelstrom.StarSystem!);
                        bool isBacktrack = skippedAttacks.Contains(attack.VictimSystem.SystemAddress);
                        if (alertPossible)
                        {
                            if (maelstromAlertCount >= 4 && !isBacktrack && backtracks == 0)
                            {
                                alertPossible = false;
                            }
                            else
                            {
                                attackingCredits -= attackCost;
                                maelstromAlertCount++;

                                if (isBacktrack)
                                {
                                    backtracks++;
                                }
                                else
                                {
                                    skippedAttacks.AddRange(attackerAttacks.Where(a => a.VictimSystem.SystemAddress != attack.VictimSystem.SystemAddress).Select(a => a.VictimSystem.SystemAddress));
                                }
                            }
                        }

                        await ProcessAttack(attack, alertPossible, maelstrom, remainingAttacker, dbContext, cancellationToken);
                        possibleAttacks.Remove(attack);
                    }

                    List<Attack> remainingAttackList = possibleAttacks.OrderBy(p => p.VictimSystem.DistanceTo(maelstrom.StarSystem!)).ToList();
                    foreach (Attack attack in remainingAttackList)
                    {
                        await ProcessAttack(attack, false, maelstrom, null, dbContext, cancellationToken);
                        possibleAttacks.Remove(attack);
                    }
                }
            }

            dbContext.AlertPredictions.RemoveRange(alertPredictions.Where(a => !UsedAlertPredictions.Contains(a)));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task ProcessAttack(Attack attack,bool alertPossible, ThargoidMaelstrom maelstrom, StarSystemCycleState? primaryAttacker, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            EDDatabase.AlertPrediction? systemAlertPrediction = await dbContext.AlertPredictions
                .AsSingleQuery()
                .Include(a => a.Attackers!)
                .Where(a => a.Cycle == ThargoidCycle && a.StarSystemId == attack.VictimSystem.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (systemAlertPrediction == null)
            {
                systemAlertPrediction = new(0, attack.VictimSystem.Id, alertPossible, AlertPredictionStatus.Default)
                {
                    Cycle = ThargoidCycle,
                    Maelstrom = maelstrom,
                    Attackers = new(),
                };
                dbContext.AlertPredictions.Add(systemAlertPrediction);
            }
            systemAlertPrediction.AlertLikely = alertPossible;
            UsedAlertPredictions.Add(systemAlertPrediction);

            List<AlertPredictionAttacker> usedAttackers = new();
            List<StarSystemCycleState> attackers = attack.Attackers
                .OrderBy(a => PrimaryAttackerSystems.Contains(a))
                .ThenByDescending(a => a.SystemAddress == primaryAttacker?.SystemAddress)
                .ThenBy(a => a.DistanceTo(maelstrom.StarSystem!))
                .ToList();
            int order = 0;
            foreach (StarSystemCycleState attacker in attackers)
            {
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

            if (primaryAttacker != null)
            {
                PrimaryAttackerSystems.Add(primaryAttacker);
            }
        }
    }
}
