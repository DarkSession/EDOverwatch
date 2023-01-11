namespace EDDataProcessor.Inara
{
    internal class UpdateFromInara
    {
        private EdDbContext DbContext { get; }
        private InaraClient InaraClient { get; }
        private ILogger Log { get; }
        public UpdateFromInara(EdDbContext dbContext, InaraClient inaraClient, ILogger<UpdateFromInara> log)
        {
            DbContext = dbContext;
            InaraClient = inaraClient;
            Log = log;
        }

        public async IAsyncEnumerable<long> Update()
        {
            await foreach ((int systemId, string systemName) in InaraClient.GetThargoidConflictList())
            {
                StarSystem? starSystem = await DbContext.StarSystems.FirstOrDefaultAsync(s => s.Name == systemName && s.WarRelevantSystem);
                if (starSystem != null)
                {
                    ConflictDetails conflictDetails = await InaraClient.GetConflictDetails(systemId);
                    foreach ((DateOnly date, int kills, int rescues, int supplies) in conflictDetails.Details)
                    {
                        List<WarEffort> warEfforts = await DbContext.WarEfforts
                                                                        .Where(w =>
                                                                            w.StarSystem == starSystem &&
                                                                            w.Date == date &&
                                                                            w.Source == WarEffortSource.Inara)
                                                                        .ToListAsync();
                        if (kills > 0)
                        {
                            WarEffort? warEffortKills = warEfforts.FirstOrDefault(w => w.Type == WarEffortType.KillGeneric && w.Side == WarEffortSide.Humans);
                            if (warEffortKills != null)
                            {
                                if (warEffortKills.Amount != kills)
                                {
                                    warEffortKills.Amount = kills;
                                    yield return starSystem.SystemAddress;
                                }
                            }
                            else
                            {
                                warEffortKills = new(0, WarEffortType.KillGeneric, date, kills, WarEffortSide.Humans, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffortKills);
                                yield return starSystem.SystemAddress;
                            }
                        }
                        if (rescues > 0)
                        {
                            WarEffort? warEffortRescues = warEfforts.FirstOrDefault(w => w.Type == WarEffortType.Rescue && w.Side == WarEffortSide.Humans);
                            if (warEffortRescues != null)
                            {
                                if (warEffortRescues.Amount != rescues)
                                {
                                    warEffortRescues.Amount = rescues;
                                    yield return starSystem.SystemAddress;
                                }
                            }
                            else
                            {
                                warEffortRescues = new(0, WarEffortType.Rescue, date, rescues, WarEffortSide.Humans, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffortRescues);
                                yield return starSystem.SystemAddress;
                            }
                        }
                        if (supplies > 0)
                        {
                            WarEffort? warEffortSupplies = warEfforts.FirstOrDefault(w => w.Type == WarEffortType.SupplyDelivery && w.Side == WarEffortSide.Humans);
                            if (warEffortSupplies != null)
                            {
                                if (warEffortSupplies.Amount != supplies)
                                {
                                    warEffortSupplies.Amount = supplies;
                                    yield return starSystem.SystemAddress;
                                }
                            }
                            else
                            {
                                warEffortSupplies = new(0, WarEffortType.SupplyDelivery, date, supplies, WarEffortSide.Humans, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffortSupplies);
                                yield return starSystem.SystemAddress;
                            }
                        }
                    }

                    await DbContext.SaveChangesAsync();

                    List<(WarEffortType warEffortType, WarEffortSide side, long newTotal)> newTotals = new();
                    if (conflictDetails.TotalThargoidsKilled > 0)
                    {
                        newTotals.Add((WarEffortType.KillGeneric, WarEffortSide.Humans, conflictDetails.TotalThargoidsKilled));
                    }
                    if (conflictDetails.TotalSuppliesDelivered > 0)
                    {
                        newTotals.Add((WarEffortType.SupplyDelivery, WarEffortSide.Humans, conflictDetails.TotalSuppliesDelivered));
                    }
                    if (conflictDetails.TotalRescuesPerformed > 0)
                    {
                        newTotals.Add((WarEffortType.Rescue, WarEffortSide.Humans, conflictDetails.TotalRescuesPerformed));
                    }
                    foreach ((WarEffortType warEffortType, WarEffortSide side, long newTotal) in newTotals)
                    {
                        long currentTotal = await DbContext.WarEfforts
                            .Where(w =>
                                    w.StarSystem == starSystem &&
                                    w.Type == warEffortType &&
                                    w.Side == side &&
                                    w.Source == WarEffortSource.Inara)
                            .SumAsync(w => w.Amount);
                        if (currentTotal != newTotal)
                        {
                            DateOnly date = new(2022, 11, 1);
                            WarEffort? warEffort = await DbContext.WarEfforts
                                .FirstOrDefaultAsync(w =>
                                        w.StarSystem == starSystem &&
                                        w.Type == warEffortType &&
                                        w.Side == side &&
                                        w.Source == WarEffortSource.Inara &&
                                        w.Date == date);
                            if (warEffort != null)
                            {
                                warEffort.Amount += (newTotal - currentTotal);
                            }
                            else
                            {
                                warEffort = new(0, warEffortType, date, (newTotal - currentTotal), side, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(warEffort);
                            }
                            yield return starSystem.SystemAddress;
                        }
                    }
                    if (conflictDetails.TotalShipsLost > 0)
                    {
                        long currentTotalShipsLost = await DbContext.WarEfforts
                            .Where(w =>
                                    w.StarSystem == starSystem &&
                                    w.Type == WarEffortType.KillGeneric &&
                                    w.Side == WarEffortSide.Thargoids &&
                                    w.Source == WarEffortSource.Inara)
                            .SumAsync(w => w.Amount);
                        if (currentTotalShipsLost != conflictDetails.TotalShipsLost)
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
                                shipKills.Amount += (conflictDetails.TotalShipsLost - currentTotalShipsLost);
                            }
                            else
                            {
                                shipKills = new(0, WarEffortType.KillGeneric, today, (conflictDetails.TotalShipsLost - currentTotalShipsLost), WarEffortSide.Thargoids, WarEffortSource.Inara)
                                {
                                    StarSystem = starSystem,
                                };
                                DbContext.WarEfforts.Add(shipKills);
                            }
                            yield return starSystem.SystemAddress;
                        }
                    }
                    await DbContext.SaveChangesAsync();
                }
            }

            int requests = InaraClient.ResetRequestCount();
            Log.LogInformation("Requests sent: {requests}", requests);
        }
    }
}
