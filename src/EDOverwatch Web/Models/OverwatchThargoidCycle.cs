using Newtonsoft.Json;

namespace EDOverwatch_Web.Models
{
    public class OverwatchThargoidCycle
    {
        [JsonIgnore]
        public int Id { get; set; }
        public DateOnly Cycle { get; }
        public DateTimeOffset Start { get; }
        public DateOnly StartDate { get; }
        public DateTimeOffset End { get; }
        public DateOnly EndDate { get; }
        public bool IsCurrent { get; }

        public OverwatchThargoidCycle(ThargoidCycle thargoidCycle)
        {
            Id = thargoidCycle.Id;
            Cycle = DateOnly.FromDateTime(thargoidCycle.Start.DateTime);
            Start = thargoidCycle.Start;
            StartDate = DateOnly.FromDateTime(Start.DateTime);
            End = thargoidCycle.End;
            EndDate = DateOnly.FromDateTime(End.DateTime);
            IsCurrent = WeeklyTick.GetLastTick() == thargoidCycle.Start;
        }

        public static async Task<List<OverwatchThargoidCycle>> GetThargoidCycles(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidCycle> thargoidCycles = await dbContext.ThargoidCycles
                .AsNoTracking()
                .Where(t => t.Start <= DateTimeOffset.UtcNow && t.Start >= new DateTimeOffset(2022, 11, 24, 7, 0, 0, TimeSpan.Zero))
                .OrderBy(t => t.Start)
                .ToListAsync(cancellationToken);
            return thargoidCycles.Select(t => new OverwatchThargoidCycle(t)).ToList();
        }

        public static async Task<List<OverwatchThargoidCycle>> GetRecentThargoidCycles(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateTimeOffset recentStart = WeeklyTick.GetLastTick().AddDays(-28);
            List<ThargoidCycle> thargoidCycles = await dbContext.ThargoidCycles
                .AsNoTracking()
                .Where(t => t.Start <= DateTimeOffset.UtcNow && t.Start >= recentStart)
                .OrderBy(t => t.Start)
                .ToListAsync(cancellationToken);
            return thargoidCycles.Select(t => new OverwatchThargoidCycle(t)).ToList();
        }
    }
}
