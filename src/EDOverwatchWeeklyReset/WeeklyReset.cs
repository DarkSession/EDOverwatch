using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDDataProcessor
{
    public static class WeeklyReset
    {
        public static async Task ProcessWeeklyReset(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            IQueryable<StarSystem> starSystemPreQuery = dbContext.StarSystems
                        .Include(s => s.ThargoidLevel)
                        .Include(s => s.ThargoidLevel!.Maelstrom)
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel != null &&
                        s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                        s.ThargoidLevel.CycleEnd == null);

            // We start with all the system which are at the end of the timer but did not progress to 100%
            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Include(s => s.ThargoidLevel)
                    .Include(s => s.ThargoidLevel!.Maelstrom)
                    .Include(s => s.ThargoidLevel!.StateExpires)
                    .Where(s =>
                        (s.ThargoidLevel!.StateExpires!.End <= currentThargoidCycle.Start ||
                        (s.ThargoidLevel!.StateExpires == null && s.ThargoidLevel.State == StarSystemThargoidLevelState.Alert)) &&
                        s.ThargoidLevel.Progress != 100 &&
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Maelstrom && // Or systems with a maelstrom
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Recovery // We do not yet know what happens if we fail to complete systems in recovery
                        )
                    .ToListAsync(cancellationToken);
                foreach (StarSystem starSystem in starSystems)
                {
                    if (starSystem.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled)
                    {
                        // If its controlled, we just reset the timer
                        starSystem.ThargoidLevel.StateExpires = null;
                        continue;
                    }

                    StarSystemThargoidLevel oldThargoidLevel = starSystem.ThargoidLevel!;
                    oldThargoidLevel.CycleEnd = currentThargoidCycle;

                    StarSystemThargoidLevelState newState = oldThargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.Alert when starSystem.Population == 0 => StarSystemThargoidLevelState.Controlled,
                        StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.Invasion,
                        StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Controlled,
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

                    if (newState == StarSystemThargoidLevelState.Controlled)
                    {
                        starSystem.Population = 0;
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Next we update all the systems which have been cleared in the previous week to their new state
            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s =>
                        s.ThargoidLevel!.Progress == 100)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    StarSystemThargoidLevel oldThargoidLevel = starSystem.ThargoidLevel!;
                    oldThargoidLevel.CycleEnd = currentThargoidCycle;

                    StarSystemThargoidLevelState newState = oldThargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                        StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recovery,
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

                    switch (newState)
                    {
                        case StarSystemThargoidLevelState.Recovery:
                            {
                                List<Station> stations = await dbContext.Stations
                                    .Where(s => s.StarSystem == starSystem && s.State != StationState.Normal && s.Updated < currentThargoidCycle.Start)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.UnderRepairs;
                                    station.Updated = currentThargoidCycle.Start;
                                }

                                List<DcohFactionOperation> factionOperations = await dbContext.DcohFactionOperations
                                    .Where(s =>
                                        s.StarSystem == starSystem &&
                                        s.Status == DcohFactionOperationStatus.Active &&
                                        s.Type != DcohFactionOperationType.Logistics)
                                    .ToListAsync(cancellationToken);
                                foreach (DcohFactionOperation factionOperation in factionOperations)
                                {
                                    factionOperation.Status = DcohFactionOperationStatus.Expired;
                                }
                                break;
                            }
                        case StarSystemThargoidLevelState.None when oldThargoidLevel.State == StarSystemThargoidLevelState.Recovery:
                            {
                                List<Station> stations = await dbContext.Stations
                                    .Where(s => s.StarSystem == starSystem && s.State != StationState.Normal && s.Updated < currentThargoidCycle.Start)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.Normal;
                                    station.Updated = currentThargoidCycle.Start;
                                }

                                List<DcohFactionOperation> factionOperations = await dbContext.DcohFactionOperations
                                    .Where(s =>
                                        s.StarSystem == starSystem &&
                                        s.Status == DcohFactionOperationStatus.Active)
                                    .ToListAsync(cancellationToken);
                                foreach (DcohFactionOperation factionOperation in factionOperations)
                                {
                                    factionOperation.Status = DcohFactionOperationStatus.Expired;
                                }
                                break;
                            }
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            dbContext.ChangeTracker.Clear();

            // Next, we change the station states for systems in invasion
            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);
                List<Station> stations = await dbContext.Stations
                    .Where(s =>
                        s.StarSystem!.WarRelevantSystem &&
                        s.StarSystem.ThargoidLevel != null &&
                        s.StarSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Invasion &&
                        (s.State == StationState.UnderAttack || s.State == StationState.Damaged) &&
                        s.Updated < currentThargoidCycle.Start)
                    .ToListAsync(cancellationToken);
                foreach (Station station in stations)
                {
                    station.State = station.State switch
                    {
                        StationState.UnderAttack => StationState.Damaged,
                        StationState.Damaged => StationState.Abandoned,
                        _ => station.State,
                    };
                    station.Updated = currentThargoidCycle.Start;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Last we reset the progress for all systems except systems in recovery
            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s =>
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Recovery &&
                        s.ThargoidLevel.Progress != null && s.ThargoidLevel.Progress != 0)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    starSystem.ThargoidLevel!.Progress = 0;
                    starSystem.ThargoidLevel!.CurrentProgress = null;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
        }
    }
}
