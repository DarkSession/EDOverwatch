namespace EDOverwatch_Web.Models
{
    public class OverwatchOverview
    {
        public OverwatchOverviewHuman? Humans { get; set; }
        public OverwatchOverviewThargoids? Thargoids { get; set; }
        public OverwatchOverviewContested? Contested { get; set; }

        public static async Task<OverwatchOverview> LoadOverwatchOverview(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchOverview result = new();
            int relevantSystemCount = await dbContext.StarSystems
                .Where(s =>
                    s.WarRelevantSystem &&
                    (s.Population > 0 ||
                        (s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled ||
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Maelstrom)))
                .CountAsync(cancellationToken);
            if (relevantSystemCount == 0)
            {
                relevantSystemCount = 1;
            }

            {
                int thargoidsSystemsControlling = await dbContext.StarSystems
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled)
                    .CountAsync(cancellationToken);
                var warEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w =>
                            w.Side == WarEffortSide.Thargoids && 
                            w.StarSystem!.WarRelevantSystem && 
                            w.StarSystem!.ThargoidLevel != null)
                    .GroupBy(w => w.Type)
                    .Select(w => new
                    {
                        type = w.Key,
                        amount = w.Sum(s => s.Amount)
                    })
                    .ToListAsync(cancellationToken);
                result.Thargoids = new(
                    Math.Round((double)thargoidsSystemsControlling / (double)relevantSystemCount, 4),
                    await dbContext.ThargoidMaelstroms.CountAsync(cancellationToken),
                    thargoidsSystemsControlling,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.KillGeneric)?.amount
                );
            }
            {
                int humansSystemsControlling = await dbContext.StarSystems
                    .Where(s =>
                        s.WarRelevantSystem &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Controlled &&
                        s.ThargoidLevel!.State != StarSystemThargoidLevelState.Maelstrom &&
                    s.Population > 0)
                    .CountAsync(cancellationToken);

                var warEfforts = await dbContext.WarEfforts
                    .AsNoTracking()
                    .Where(w => 
                            w.Side == WarEffortSide.Humans && 
                            w.StarSystem!.WarRelevantSystem && 
                            w.StarSystem!.ThargoidLevel != null)
                    .GroupBy(w => w.Type)
                    .Select(w => new
                    {
                        type = w.Key,
                        amount = w.Sum(s => s.Amount)
                    })
                    .ToListAsync(cancellationToken);

                List<WarEffortType> warEffortTypeKills = new()
                {
                    WarEffortType.KillGeneric,
                    WarEffortType.KillThargoidScout,
                    WarEffortType.KillThargoidCyclops,
                    WarEffortType.KillThargoidBasilisk,
                    WarEffortType.KillThargoidMedusa,
                    WarEffortType.KillThargoidHydra,
                    WarEffortType.KillThargoidOrthrus,
                };

                List<WarEffortType> warEffortTypeMissions = new()
                {
                    WarEffortType.MissionCompletionGeneric,
                    WarEffortType.MissionCompletionDelivery,
                    WarEffortType.MissionCompletionRescue,
                    WarEffortType.MissionCompletionThargoidKill,
                    WarEffortType.MissionCompletionPassengerEvacuation,
                };

                result.Humans = new(
                    Math.Round((double)humansSystemsControlling / (double)relevantSystemCount, 4),
                    humansSystemsControlling,
                    0,
                    warEfforts.FirstOrDefault(w => warEffortTypeKills.Contains(w.type))?.amount,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.Rescue)?.amount,
                    warEfforts.FirstOrDefault(w => w.type == WarEffortType.SupplyDelivery)?.amount,
                    warEfforts.FirstOrDefault(w => warEffortTypeMissions.Contains(w.type))?.amount);
            }
            result.Contested = new(
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion).CountAsync(cancellationToken),
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert).CountAsync(cancellationToken),
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recapture).CountAsync(cancellationToken),
                await dbContext.StarSystems.Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recovery).CountAsync(cancellationToken)
            );
            return result;
        }
    }
}
