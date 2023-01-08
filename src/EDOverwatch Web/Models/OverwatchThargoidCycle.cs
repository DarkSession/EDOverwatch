namespace EDOverwatch_Web.Models
{
    public class OverwatchThargoidCycle
    {
        public DateOnly Cycle { get; }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public bool IsCurrent { get; }

        public OverwatchThargoidCycle(ThargoidCycle thargoidCycle)
        {
            Cycle = DateOnly.FromDateTime(thargoidCycle.Start.DateTime);
            Start = thargoidCycle.Start;
            End = thargoidCycle.End;
            IsCurrent = WeeklyTick.GetLastTick() == thargoidCycle.Start;
        }

        public static async Task<List<OverwatchThargoidCycle>> GetThargoidCycles(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidCycle> thargoidCycles = await dbContext.ThargoidCycles
                .AsNoTracking()
                .Where(t => t.Start <= DateTimeOffset.UtcNow && t.Start >= new DateTimeOffset(2022, 12, 1, 7, 0, 0, TimeSpan.Zero))
                .OrderBy(t => t.Start)
                .ToListAsync(cancellationToken);
            return thargoidCycles.Select(t => new OverwatchThargoidCycle(t)).ToList();
        }
    }
}
