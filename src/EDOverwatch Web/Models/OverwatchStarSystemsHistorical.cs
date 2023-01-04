namespace EDOverwatch_Web.Models
{
    public class OverwatchStarSystemsHistorical
    {
        public List<OverwatchMaelstrom> Maelstroms { get; }
        public List<OverwatchThargoidLevel> Levels => Enum.GetValues<StarSystemThargoidLevelState>()
            .Where(s => s > StarSystemThargoidLevelState.None)
            .Select(s => new OverwatchThargoidLevel(s))
            .ToList();
        public List<OverwatchStarSystemsHistoricalSystem> Systems { get; } = new();

        public OverwatchStarSystemsHistorical(List<ThargoidMaelstrom> thargoidMaelstroms)
        {
            Maelstroms = thargoidMaelstroms.Select(t => new OverwatchMaelstrom(t)).ToList();
        }

        public static async Task<OverwatchStarSystemsHistorical> Create(DateOnly date, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateTimeOffset tickTime = WeeklyTick.GetTickTime(date);
            ThargoidCycle thargoidCycle = await dbContext.GetThargoidCycle(tickTime, cancellationToken, 0);
            ThargoidCycle previousThargoidCycle = await dbContext.GetThargoidCycle(tickTime, cancellationToken, -1);

            List<StarSystem> systems = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevelHistory!
                           .Any(t =>
                                    t.Maelstrom != null && t.State > StarSystemThargoidLevelState.None && 
                                    (t.CycleEnd == null || t.CycleEnd.Start >= t.CycleStart!.Start) &&
                                    (t.CycleEnd == null ||
                                    ((t.CycleStart!.Start >= previousThargoidCycle.Start || t.CycleEnd == previousThargoidCycle) &&
                                    t.CycleEnd.End <= thargoidCycle.End))))
                .Include(s => s.ThargoidLevel!.Maelstrom!.StarSystem)
                .Include(s => s.ThargoidLevelHistory!
                                .Where(t =>
                                    t.Maelstrom != null && t.State > StarSystemThargoidLevelState.None &&
                                    (t.CycleEnd == null || t.CycleEnd.Start >= t.CycleStart!.Start) &&
                                    (t.CycleEnd == null ||
                                    ((t.CycleStart!.Start >= previousThargoidCycle.Start || t.CycleEnd == previousThargoidCycle) &&
                                    t.CycleEnd.End <= thargoidCycle.End))))
                .ThenInclude(t => t.CycleStart)
                .Include(s => s.ThargoidLevelHistory!
                                .Where(t =>
                                    t.Maelstrom != null && t.State > StarSystemThargoidLevelState.None &&
                                    (t.CycleEnd == null || t.CycleEnd.Start >= t.CycleStart!.Start) &&
                                    (t.CycleEnd == null ||
                                    ((t.CycleStart!.Start >= previousThargoidCycle.Start || t.CycleEnd == previousThargoidCycle) &&
                                    t.CycleEnd.End <= thargoidCycle.End))))
                .ThenInclude(t => t.CycleEnd)
                .Include(s => s.ThargoidLevelHistory!
                                .Where(t =>
                                    t.Maelstrom != null && t.State > StarSystemThargoidLevelState.None &&
                                    (t.CycleEnd == null || t.CycleEnd.Start >= t.CycleStart!.Start) &&
                                    (t.CycleEnd == null ||
                                    ((t.CycleStart!.Start >= previousThargoidCycle.Start || t.CycleEnd == previousThargoidCycle) &&
                                    t.CycleEnd.End <= thargoidCycle.End))))
                .ThenInclude(t => t.Maelstrom)
                .ThenInclude(m => m!.StarSystem)
                .ToListAsync(cancellationToken);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Include(t => t.StarSystem)
                .ToListAsync(cancellationToken);

            OverwatchStarSystemsHistorical result = new(maelstroms);
            foreach (StarSystem system in systems)
            {
                OverwatchStarSystemsHistoricalSystem overwatchStarSystemsHistoricalSystem = new(system, thargoidCycle, previousThargoidCycle);
                result.Systems.Add(overwatchStarSystemsHistoricalSystem);
            }
            return result;
        }
    }
}
