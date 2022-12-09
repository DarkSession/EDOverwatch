namespace EDDataProcessor.Inara
{
    internal class UpdateFromInara
    {
        private EdDbContext DbContext { get; }
        private InaraClient InaraClient { get; }
        public UpdateFromInara(EdDbContext dbContext, InaraClient inaraClient)
        {
            DbContext = dbContext;
            InaraClient = inaraClient;
        }

        public async Task Update()
        {
            await foreach ((int systemId, string systemName) in InaraClient.GetThargoidConflictList())
            {
                StarSystem? starSystem = await DbContext.StarSystems.FirstOrDefaultAsync(s => s.Name == systemName && s.WarRelevantSystem);
                if (starSystem != null)
                {
                    ConflictDetails conflictDetails = await InaraClient.GetConflictDetails(systemId);
                    if (conflictDetails.ShipsLost > 0)
                    {
                        long currentTotalShipsLost = await DbContext.WarEfforts
                            .Where(w =>
                                    w.StarSystem == starSystem &&
                                    w.Type == WarEffortType.KillGeneric &&
                                    w.Side == WarEffortSide.Thargoids &&
                                    w.Source == WarEffortSource.Inara)
                            .SumAsync(w => w.Amount);
                        if (currentTotalShipsLost != conflictDetails.ShipsLost)
                        {
                            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
                            WarEffort? shipKills = await DbContext.WarEfforts
                                .FirstOrDefaultAsync(w =>
                                        w.StarSystem == starSystem &&
                                        w.Type == WarEffortType.KillGeneric &&
                                        w.Side == WarEffortSide.Thargoids &&
                                        w.Source == WarEffortSource.Inara &&
                                        w.Date == today);
                            if (shipKills != null)
                            {
                                shipKills.Amount += (conflictDetails.ShipsLost - currentTotalShipsLost);
                            }
                            else
                            {
                                shipKills = new(0, WarEffortType.KillGeneric, today, (conflictDetails.ShipsLost - currentTotalShipsLost), WarEffortSide.Thargoids, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(shipKills);
                            }
                        }
                    }
                    foreach ((DateOnly date, int scoutKills, int interceptorKills, int rescues) in conflictDetails.Details)
                    {
                        List<WarEffort> warEfforts = await DbContext.WarEfforts
                                                                        .Where(w =>
                                                                            w.StarSystem == starSystem &&
                                                                            w.Date == date &&
                                                                            w.Source == WarEffortSource.Inara)
                                                                        .ToListAsync();
                        long kills = (scoutKills + interceptorKills);
                        if (kills > 0)
                        {
                            WarEffort? warEffortKills = warEfforts.FirstOrDefault(w => w.Type == WarEffortType.KillGeneric && w.Side == WarEffortSide.Humans);
                            if (warEffortKills != null)
                            {
                                warEffortKills.Amount = kills;
                            }
                            else
                            {
                                warEffortKills = new(0, WarEffortType.KillGeneric, date, kills, WarEffortSide.Humans, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffortKills);
                            }
                        }
                        if (rescues > 0)
                        {
                            WarEffort? warEffortRescues = warEfforts.FirstOrDefault(w => w.Type == WarEffortType.Rescue && w.Side == WarEffortSide.Humans);
                            if (warEffortRescues != null)
                            {
                                warEffortRescues.Amount = rescues;
                            }
                            else
                            {
                                warEffortRescues = new(0, WarEffortType.Rescue, date, rescues, WarEffortSide.Humans, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffortRescues);
                            }
                        }
                    }
                    await DbContext.SaveChangesAsync();
                }
            }
        }
    }
}
