using EDDatabase;
using Microsoft.EntityFrameworkCore;

namespace EDDataProcessor
{
    public static class WeeklyReset
    {
        public static async Task ProcessWeeklyReset(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken);

            {
                List<StarSystem> starSystems = await dbContext.StarSystems
                    .Include(s => s.ThargoidLevel)
                    .Include(s => s.ThargoidLevel!.Maelstrom)
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel != null &&
                        s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                        s.ThargoidLevel.CycleEnd == null &&
                        s.ThargoidLevel.Progress == 100)
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
                        case StarSystemThargoidLevelState.Controlled:
                            {

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

            {
                List<StarSystem> starSystems = await dbContext.StarSystems
                    .Include(s => s.ThargoidLevel)
                    .Include(s => s.ThargoidLevel!.Maelstrom)
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel != null &&
                        s.ThargoidLevel.State > StarSystemThargoidLevelState.None &&
                        s.ThargoidLevel.State != StarSystemThargoidLevelState.Recovery &&
                        s.ThargoidLevel.CycleEnd == null &&
                        s.ThargoidLevel.Progress != null)
                    .ToListAsync(cancellationToken);

                foreach (StarSystem starSystem in starSystems)
                {
                    starSystem.ThargoidLevel!.Progress = null;
                    starSystem.ThargoidLevel!.CurrentProgress = null;

                    switch (starSystem.ThargoidLevel.State)
                    {
                        case StarSystemThargoidLevelState.Invasion:
                            {
                                List<Station> stations = await dbContext.Stations
                                    .Where(s => s.StarSystem == starSystem && s.State == StationState.UnderAttack)
                                    .ToListAsync(cancellationToken);
                                foreach (Station station in stations)
                                {
                                    station.State = StationState.Abandoned;
                                }
                                break;
                            }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
