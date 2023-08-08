namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemsHistorical
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>()
            .Where(s => s > StarSystemThargoidLevelState.None)
            .Select(s => new OverwatchThargoidLevel(s))
            .ToList();
        public List<OverwatchStarSystemsHistoricalSystem> Systems { get; set; } = new();
        public List<OverwatchThargoidCycle> ThargoidCycles { get; } = new();
        public OverwatchThargoidCycle Cycle { get; }

        public OverwatchStarSystemsHistorical(List<ThargoidMaelstrom> thargoidMaelstroms, ThargoidCycle thargoidCycle)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
            Cycle = new(thargoidCycle);
        }

        public static async Task<OverwatchStarSystemsHistorical> Create(DateOnly? cycle, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateOnly date;
            if (cycle == null || cycle >= DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date) || !await dbContext.ThargoidCycleExists((DateOnly)cycle, cancellationToken))
            {
                date = DateOnly.FromDateTime(WeeklyTick.GetTickTime(DateTimeOffset.UtcNow).DateTime);
            }
            else
            {
                date = (DateOnly)cycle;
            }

            DateTimeOffset tickTime = WeeklyTick.GetTickTime(date);
            ThargoidCycle thargoidCycle = await dbContext.GetThargoidCycle(tickTime, cancellationToken);
            ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(tickTime, cancellationToken, -1);

            List<StarSystem> systems = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevelHistory!
                           .Any(t => t.Maelstrom != null && t.State > StarSystemThargoidLevelState.None &&
                                    (
                                        t.CycleEnd == null ||
                                        (t.CycleStart!.Start >= previousThargoidCycle.Start && t.CycleStart.Start <= thargoidCycle.Start) ||
                                        (t.CycleEnd!.End >= thargoidCycle.Start && t.CycleEnd.End <= thargoidCycle.End) ||
                                        (t.CycleStart.Start <= previousThargoidCycle.Start && t.CycleEnd.End >= thargoidCycle.End)
                                    )))
                .Include(s => s.ThargoidLevel)
                .Include(s => s.ThargoidLevelHistory!
                                .Where(t => t.Maelstrom != null &&
                                    (
                                        t.CycleEnd == null ||
                                        (t.CycleStart!.Start >= previousThargoidCycle.Start && t.CycleStart.Start <= thargoidCycle.Start) ||
                                        (t.CycleEnd!.End >= thargoidCycle.Start && t.CycleEnd.End <= thargoidCycle.End) ||
                                        (t.CycleStart.Start <= previousThargoidCycle.Start && t.CycleEnd.End >= thargoidCycle.End)
                                    )))
                .ThenInclude(t => t.ProgressHistory!.Where(p => p.Updated <= thargoidCycle.End).OrderByDescending(p => p.Updated)) // .Take(1)
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            OverwatchStarSystemsHistorical result = new(maelstroms, thargoidCycle);
            foreach (StarSystem system in systems)
            {
                system.ThargoidLevelHistory!.RemoveAll(t => t.CycleEnd == null && t.CycleStart!.Start >= thargoidCycle.End);
                foreach (StarSystemThargoidLevel historicalThargoidLevel in system.ThargoidLevelHistory)
                {
                    historicalThargoidLevel.CycleEnd ??= new(0, WeeklyTick.GetLastTick(), WeeklyTick.GetLastTick().AddDays(7));
                }
                if (system.ThargoidLevelHistory.Any(t => t.State != StarSystemThargoidLevelState.None))
                {
                    OverwatchStarSystemsHistoricalSystem overwatchStarSystemsHistoricalSystem = new(system, thargoidCycle);
                    result.Systems.Add(overwatchStarSystemsHistoricalSystem);
                }
            }

            result.ThargoidCycles.AddRange(await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken));

            return result;
        }
    }
}
