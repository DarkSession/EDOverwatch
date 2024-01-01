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
                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, -1);
                ThargoidCycle newThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);
                List<StarSystem> starSystems = await starSystemPreQuery
                    .Include(s => s.ThargoidLevel)
                    .Include(s => s.ThargoidLevel!.Maelstrom)
                    .Include(s => s.ThargoidLevel!.StateExpires)
                    .Where(s =>
                        (s.ThargoidLevel!.StateExpires!.End <= newThargoidCycle.Start ||
                        (s.ThargoidLevel!.StateExpires == null && s.ThargoidLevel.State == StarSystemThargoidLevelState.Alert)) &&
                        s.ThargoidLevel.Progress != 100 &&
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Titan && // Systems with a maelstrom
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
                    oldThargoidLevel.CycleEnd = previousThargoidCycle;

                    StarSystemThargoidLevelState newState = oldThargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.Alert when starSystem.Population == 0 => StarSystemThargoidLevelState.Controlled,
                        StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.Invasion,
                        StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Controlled,
                        _ => StarSystemThargoidLevelState.None,
                    };
                    StarSystemThargoidLevel starSystemThargoidLevel = new(0, newState, null, DateTimeOffset.UtcNow, oldThargoidLevel.IsInvisibleState, false)
                    {
                        StarSystem = starSystem,
                        CycleStart = newThargoidCycle,
                        Maelstrom = oldThargoidLevel.Maelstrom,
                    };
                    dbContext.StarSystemThargoidLevels.Add(starSystemThargoidLevel);
                    starSystem.ThargoidLevel = starSystemThargoidLevel;

                    if (newState == StarSystemThargoidLevelState.Controlled)
                    {
                        starSystem.Population = 0;
                        starSystem.PopulationMin = 0;
                        await dbContext.Stations
                            .Where(s => s.StarSystem == starSystem && s.State != StationState.Abandoned && s.Type!.Name != StationType.FleetCarrierStationType)
                            .ForEachAsync((s) =>
                            {
                                s.State = StationState.Abandoned;
                            }, cancellationToken);
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Next we update all the systems which have been cleared in the previous week to their new state
            {
                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, -1);
                ThargoidCycle newThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s => s.ThargoidLevel!.Progress == 100)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    StarSystemThargoidLevel oldThargoidLevel = starSystem.ThargoidLevel!;
                    oldThargoidLevel.CycleEnd = previousThargoidCycle;

                    StarSystemThargoidLevelState newState = oldThargoidLevel.State switch
                    {
                        StarSystemThargoidLevelState.Alert => StarSystemThargoidLevelState.None,
                        StarSystemThargoidLevelState.Invasion => StarSystemThargoidLevelState.Recovery,
                        StarSystemThargoidLevelState.Recovery => StarSystemThargoidLevelState.None,
                        StarSystemThargoidLevelState.Controlled when starSystem.OriginalPopulation > 0 => StarSystemThargoidLevelState.Recovery,
                        _ => StarSystemThargoidLevelState.None,
                    };
                    StarSystemThargoidLevel newStarSystemThargoidLevel = new(0, newState, null, DateTimeOffset.UtcNow, false, false)
                    {
                        StarSystem = starSystem,
                        CycleStart = newThargoidCycle,
                        Maelstrom = oldThargoidLevel.Maelstrom,
                    };
                    dbContext.StarSystemThargoidLevels.Add(newStarSystemThargoidLevel);
                    starSystem.ThargoidLevel = newStarSystemThargoidLevel;

                    switch (newState)
                    {
                        case StarSystemThargoidLevelState.Recovery:
                            {
                                List<Station> stations = await dbContext.Stations
                                    .Where(s => s.StarSystem == starSystem && (s.State == StationState.Abandoned || s.State == StationState.Damaged || s.State == StationState.UnderAttack) && s.Updated < newThargoidCycle.Start)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = station.State switch
                                    {
                                        StationState.UnderAttack => StationState.Normal,
                                        _ => StationState.UnderRepairs,
                                    };
                                    station.Updated = newThargoidCycle.Start;
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
                                    .Where(s => s.StarSystem == starSystem && s.State != StationState.Normal && s.Updated < newThargoidCycle.Start)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.Normal;
                                    station.Updated = newThargoidCycle.Start;
                                }

                                starSystem.Population = starSystem.OriginalPopulation;

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
                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, -1);
                ThargoidCycle newThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

                List<Station> stations = await dbContext.Stations
                    .Where(s =>
                        s.StarSystem!.WarRelevantSystem &&
                        s.StarSystem.ThargoidLevel != null &&
                        s.StarSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Invasion &&
                        (s.State == StationState.UnderAttack || s.State == StationState.Damaged) &&
                        s.Updated < newThargoidCycle.Start)
                    .ToListAsync(cancellationToken);
                foreach (Station station in stations)
                {
                    station.State = station.State switch
                    {
                        StationState.UnderAttack => StationState.Damaged,
                        StationState.Damaged => StationState.Abandoned,
                        _ => station.State,
                    };
                    station.Updated = newThargoidCycle.Start;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();

            // Now we update the progress for all systems except systems in recovery
            {
                ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, -1);
                ThargoidCycle newThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);

                List<StarSystem> starSystems = await starSystemPreQuery
                    .Where(s =>
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Recovery &&
                        s.ThargoidLevel.Progress != null && s.ThargoidLevel.Progress != 0)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    short progress;
                    if (starSystem.ThargoidLevel!.Progress <= 33)
                    {
                        starSystem.ThargoidLevel!.Progress = 0;
                        progress = 0;
                    }
                    else
                    {
                        progress = (short)starSystem.ThargoidLevel!.Progress!;
                        progress -= 33;
                        starSystem.ThargoidLevel!.Progress = progress;
                    }
                    starSystem.ThargoidLevel!.CurrentProgress = new(0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, progress, progress / 100m)
                    {
                        ThargoidLevel = starSystem.ThargoidLevel,
                    };
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
        }
    }
}
