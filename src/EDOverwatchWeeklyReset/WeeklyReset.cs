using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDDataProcessor
{
    public static class WeeklyReset
    {
        public static async Task ProcessWeeklyReset(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);

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
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s =>
                        s.ThargoidLevel!.StateExpires!.End <= currentThargoidCycle.Start &&
                        s.ThargoidLevel.Progress != 100 &&
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Controlled && // Nothing happens with controlled systems
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Maelstrom && // Or systems with a maelstrom
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Recovery // We do not yet know what happens if we fail to complete systems in recovery
                        )
                    .ToListAsync(cancellationToken);
                foreach (StarSystem starSystem in starSystems)
                {
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
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Next we update all the systems which have been cleared in the previous week to their new state
            {
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
                                    .Where(s => s.StarSystem == starSystem && s.State != StationState.Normal)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.UnderRepairs;
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
                                    .Where(s => s.StarSystem == starSystem && s.State != StationState.Normal)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.Normal;
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
                List<Station> stations = await dbContext.Stations
                    .Where(s =>
                        s.StarSystem!.WarRelevantSystem &&
                        s.StarSystem.ThargoidLevel != null &&
                        s.StarSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Invasion &&
                        (s.State == StationState.UnderAttack || s.State == StationState.Damaged))
                    .ToListAsync(cancellationToken);
                foreach (Station station in stations)
                {
                    station.State = station.State switch
                    {
                        StationState.UnderAttack => StationState.Damaged,
                        StationState.Damaged => StationState.Abandoned,
                        _ => station.State
                    };
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Last we reset the progress for all systems except systems in recovery
            {
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s =>
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Recovery &&
                        s.ThargoidLevel.Progress != null)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    starSystem.ThargoidLevel!.Progress = null;
                    starSystem.ThargoidLevel!.CurrentProgress = null;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
