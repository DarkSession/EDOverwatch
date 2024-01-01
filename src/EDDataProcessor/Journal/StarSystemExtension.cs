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
                    starSystem.ThargoidLevel = new(0, currentState, null, updateTime, false, false)
                    {
                        StarSystem = starSystem,
                        CycleStart = currentThargoidCycle,
                        Maelstrom = maelstrom,
                        StateExpires = null,
                    };
                    changed = true;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                if (currentState != StarSystemThargoidLevelState.None && (starSystem.ThargoidLevel.Progress == null || fsdJumpThargoidWar.WarProgressInternal >= starSystem.ThargoidLevel.Progress))
                {
                    if (starSystem.ThargoidLevel.Progress == null || fsdJumpThargoidWar.WarProgressInternal > starSystem.ThargoidLevel.Progress)
                    {
                        starSystem.ThargoidLevel.Progress = fsdJumpThargoidWar.WarProgressInternal;
                        starSystem.ThargoidLevel.CurrentProgress = new(0, updateTime, updateTime, fsdJumpThargoidWar.WarProgressInternal, fsdJumpThargoidWar.WarProgress)
                        {
                            ThargoidLevel = starSystem.ThargoidLevel,
                        };
                        changed = true;
                        if (starSystem.ThargoidLevel.Progress >= 100)
                        {
                            await dbContext.DcohFactionOperations
                                .Where(d =>
                                    d.StarSystem == starSystem &&
                                    d.Status == DcohFactionOperationStatus.Active)
                                .ForEachAsync(d => d.Status = DcohFactionOperationStatus.Expired, cancellationToken);
                        }
                    }
                    if (starSystem.ThargoidLevel.CurrentProgress != null && starSystem.ThargoidLevel.CurrentProgress.LastChecked < updateTime)
                    {
                        starSystem.ThargoidLevel.CurrentProgress.LastChecked = updateTime;
                    }
                    if (starSystem.ThargoidLevel.StateExpires == null && fsdJumpThargoidWar.RemainingDays is int remainingDays && remainingDays > 0)
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
    }
}
