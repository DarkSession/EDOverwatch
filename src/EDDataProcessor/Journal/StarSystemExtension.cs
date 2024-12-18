using EDDataProcessor.EDDN;

namespace EDDataProcessor.Journal
{
    internal static class StarSystemExtension
    {
        public static async Task<bool> UpdateThargoidWar(this StarSystem starSystem, DateTimeOffset updateTime, FSDJumpThargoidWar fsdJumpThargoidWar, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);
            if (updateTime <= currentThargoidCycle.Start || updateTime >= currentThargoidCycle.End || starSystem.ThargoidLevel?.ManualUpdateCycleId == currentThargoidCycle.Id || (updateTime.DayOfWeek == DayOfWeek.Thursday && updateTime.Hour == 7))
            {
                return false;
            }
            StarSystemThargoidLevelState? parsedState = fsdJumpThargoidWar.CurrentState switch
            {
                "" => StarSystemThargoidLevelState.None,
                "Thargoid_Probing" => StarSystemThargoidLevelState.Alert,
                "Thargoid_Harvest" => StarSystemThargoidLevelState.Invasion,
                "Thargoid_Controlled" => StarSystemThargoidLevelState.Controlled,
                "Thargoid_Stronghold" => StarSystemThargoidLevelState.Titan,
                "Thargoid_Recovery" => StarSystemThargoidLevelState.Recovery,
                _ => default,
            };
            if (parsedState is StarSystemThargoidLevelState currentState)
            {
                // States cannot move backward from these updates
                if (starSystem.ThargoidLevel != null && starSystem.ThargoidLevel.State > currentState)
                {
                    return false;
                }
                bool changed = false;
                if (starSystem.ThargoidLevel?.State != currentState)
                {
                    if (currentState == StarSystemThargoidLevelState.Titan && starSystem.ThargoidLevel?.Maelstrom != null && starSystem.ThargoidLevel.Maelstrom.HeartsRemaining <= 0)
                    {
                        return false;
                    }
                    if (starSystem.ThargoidLevel != null)
                    {
                        starSystem.ThargoidLevel.CycleEnd = await dbContext.GetThargoidCycle(updateTime, cancellationToken, -1);
                    }
                    ThargoidMaelstrom? maelstrom = starSystem.ThargoidLevel?.Maelstrom ?? await GetMaelstrom(starSystem, dbContext, cancellationToken);
                    if (maelstrom?.StarSystem != null)
                    {
                        decimal distanceToMaelstrom = (decimal)starSystem.DistanceTo(maelstrom.StarSystem);
                        if (distanceToMaelstrom > maelstrom.InfluenceSphere)
                        {
                            maelstrom.InfluenceSphere = distanceToMaelstrom;
                        }
                    }
                    starSystem.ThargoidLevel = new(0, currentState, null, updateTime, false, false, starSystem.ThargoidLevel?.State)
                    {
                        StarSystem = starSystem,
                        CycleStart = currentThargoidCycle,
                        Maelstrom = maelstrom,
                        StateExpires = null,
                    };
                    changed = true;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                if (currentState != StarSystemThargoidLevelState.None)
                {
                    bool titanHeartDestroyed = fsdJumpThargoidWar.WarProgress > 0.0001m &&
                        starSystem.ThargoidLevel.CurrentProgress is not null &&
                        starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Titan &&
                        (starSystem.ThargoidLevel.CurrentProgress.ProgressPercent - fsdJumpThargoidWar.WarProgress) > 0.25m &&
                        (starSystem.ThargoidLevel.Maelstrom?.HeartsRemaining ?? 0) > 0;
                    if (starSystem.ThargoidLevel.CurrentProgress is null || fsdJumpThargoidWar.WarProgress > starSystem.ThargoidLevel.CurrentProgress.ProgressPercent || titanHeartDestroyed)
                    {
                        decimal warProgress = fsdJumpThargoidWar.WarProgress;
                        if (titanHeartDestroyed && starSystem.ThargoidLevel.Maelstrom != null)
                        {
                            short heartNumber = starSystem.ThargoidLevel.Maelstrom.HeartsRemaining;
                            ThargoidMaelstromHeart? thargoidMaelstromHeart = await dbContext.ThargoidMaelstromHearts.FirstOrDefaultAsync(t => t.Maelstrom == starSystem.ThargoidLevel.Maelstrom && t.Heart == heartNumber, cancellationToken);
                            if (thargoidMaelstromHeart is null)
                            {
                                thargoidMaelstromHeart = new(0, heartNumber, updateTime)
                                {
                                    Maelstrom = starSystem.ThargoidLevel.Maelstrom,
                                };
                                dbContext.ThargoidMaelstromHearts.Add(thargoidMaelstromHeart);
                            }
                            else
                            {
                                thargoidMaelstromHeart.DestructionTime ??= updateTime;
                            }

                            if (starSystem.ThargoidLevel.Maelstrom.HeartsRemaining < 0)
                            {
                                starSystem.ThargoidLevel.Maelstrom.HeartsRemaining = 0;
                                if (starSystem.ThargoidLevel.Maelstrom.MeltdownTimeEstimate is null)
                                {
                                    starSystem.ThargoidLevel.Maelstrom.MeltdownTimeEstimate = updateTime.AddDays(1);
                                    starSystem.ThargoidLevel.Maelstrom.DefeatCycle = currentThargoidCycle;
                                }
                                warProgress = 1m;
                            }
                        }

                        if (currentState == StarSystemThargoidLevelState.Titan && starSystem.ThargoidLevel.CurrentProgress?.ProgressPercent is decimal currentProgress)
                        {
                            decimal progressIncrease = warProgress - currentProgress;
                            if (progressIncrease > 0.5m)
                            {
                                return false;
                            }
                        }

                        short progressOld = (short)Math.Floor(warProgress * 100m);
                        if (progressOld > 100)
                        {
                            progressOld = 100;
                        }
                        starSystem.ThargoidLevel.ProgressOld = progressOld;
                        starSystem.ThargoidLevel.CurrentProgress = new(0, updateTime, updateTime, progressOld, warProgress)
                        {
                            ThargoidLevel = starSystem.ThargoidLevel,
                        };
                        if (starSystem.ThargoidLevel.CurrentProgress.IsCompleted)
                        {
                            await dbContext.DcohFactionOperations
                                .Where(d =>
                                    d.StarSystem == starSystem &&
                                    d.Status == DcohFactionOperationStatus.Active)
                                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Status, DcohFactionOperationStatus.Expired), cancellationToken);
                        }
                        changed = true;
                    }
                    else if (fsdJumpThargoidWar.WarProgress == starSystem.ThargoidLevel.CurrentProgress.ProgressPercent && starSystem.ThargoidLevel.CurrentProgress.LastChecked < updateTime)
                    {
                        starSystem.ThargoidLevel.CurrentProgress.LastChecked = updateTime;
                    }

                    if (starSystem.ThargoidLevel.StateExpires is null && fsdJumpThargoidWar.RemainingDays is int remainingDays && remainingDays > 0)
                    {
                        DateTimeOffset remainingTimeEnd = DateTimeOffset.UtcNow.AddDays(remainingDays);
                        if (remainingTimeEnd.DayOfWeek == DayOfWeek.Wednesday || (remainingTimeEnd.DayOfWeek == DayOfWeek.Thursday && remainingTimeEnd.Hour < 7))
                        {
                            remainingTimeEnd = new DateTimeOffset(remainingTimeEnd.Year, remainingTimeEnd.Month, remainingTimeEnd.Day, 0, 0, 0, TimeSpan.Zero);
                            ThargoidCycle thargoidCycle = await dbContext.GetThargoidCycle(remainingTimeEnd, CancellationToken.None);
                            starSystem.ThargoidLevel.StateExpires = thargoidCycle;
                            changed = true;
                        }
                    }
                }
                return changed;
            }
            return false;
        }

        private static async Task<ThargoidMaelstrom?> GetMaelstrom(StarSystem starSystem, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .Include(t => t.StarSystem)
                .Where(t =>
                    t.StarSystem!.LocationX >= starSystem.LocationX - (t.InfluenceSphere + 11m) && t.StarSystem!.LocationX <= starSystem.LocationX + (t.InfluenceSphere + 11m) &&
                    t.StarSystem!.LocationY >= starSystem.LocationY - (t.InfluenceSphere + 11m) && t.StarSystem!.LocationY <= starSystem.LocationY + (t.InfluenceSphere + 11m) &&
                    t.StarSystem!.LocationZ >= starSystem.LocationZ - (t.InfluenceSphere + 11m) && t.StarSystem!.LocationZ <= starSystem.LocationZ + (t.InfluenceSphere + 11m))
                .ToListAsync(cancellationToken);
            ThargoidMaelstrom? maelstrom = maelstroms
                .OrderBy(m => m.StarSystem?.DistanceTo(starSystem) ?? 999)
                .FirstOrDefault();
            return maelstrom;
        }

        private enum WarProgressChange
        {
            None,
            ProgressConfirmed,
            ProgressNewOrChanged,
        }
    }
}
