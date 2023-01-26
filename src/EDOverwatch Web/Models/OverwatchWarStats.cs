namespace EDOverwatch_Web.Models
{
    public class OverwatchWarStats
    {
        public List<OverwatchOverviewMaelstromHistoricalSummary> MaelstromHistory { get; set; } = new();
        public List<WarEffortSummary>? WarEffortSums { get; set; }

        public static async Task Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<WarEffortSummary> warEffortSums = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w => w.Cycle != null && w.Side == WarEffortSide.Humans)
                .GroupBy(w => new { w.Cycle!.Start, w.Type })
                .Select(w => new WarEffortSummary(DateOnly.FromDateTime(w.Key.Start.DateTime), w.Key.Type, w.Sum(x => x.Amount)))
                .ToListAsync(cancellationToken);

            List<StarSystemThargoidLevel> completed = await dbContext.StarSystemThargoidLevels
                .AsNoTracking()
                .Include(s => s.CycleEnd)
                .Include(s => s.StarSystem)
                .Where(s =>
                    s.Progress == 100 &&
                    (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled) &&
                    s.ProgressHistory!.Any() &&
                    s.CycleEnd != null &&
                    s.CycleEnd.Start >= new DateTimeOffset(2022, 12, 22, 7, 0, 0, TimeSpan.Zero))
                .ToListAsync(cancellationToken);

            var result = await dbContext.WarEfforts
                .Where(w => completed.Any(c => c.StarSystem == w.StarSystem && c.CycleEnd == w.Cycle) && w.Side == WarEffortSide.Humans)
                .GroupBy(w => w.Type)
                .Select(x => new
                {
                    type = x.Key,
                    // amount = x.Sum(g => g.Amount),
                })
                .ToListAsync(cancellationToken);


            /*
            var result = await dbContext.StarSystemThargoidLevels
                .AsNoTracking()
                .Where(s =>
                    s.Progress == 100 &&
                    (s.State == StarSystemThargoidLevelState.Alert || s.State == StarSystemThargoidLevelState.Invasion || s.State == StarSystemThargoidLevelState.Controlled) &&
                    s.ProgressHistory!.Any() &&
                    s.CycleEnd != null &&
                    s.CycleEnd.Start >= new DateTimeOffset(2022, 12, 22, 7, 0, 0, TimeSpan.Zero))
                .Select(s => new
                {
                    thargoidLevel = s,
                    warEfforts = s.EndCycleWarEfforts!
                        .Where(w =>
                            w.StarSystem == s.StarSystem &&
                            w.Cycle == s.CycleEnd &&
                            w.Side == WarEffortSide.Humans)
                        .GroupBy(w => w.Type)
                        .Select(x => new
                        {
                            type = x.Key,
                            // amount = x.Sum(g => g.Amount),
                        })
                        .ToList(),
                })
                .ToListAsync(cancellationToken);
            */
        }
    }

    public class WarEffortSummary
    {
        public DateOnly Cycle { get; }
        public WarEffortType TypeId { get; }
        public string Type => EnumUtil.GetEnumMemberValue(TypeId);
        public long Amount { get; }

        public WarEffortSummary(DateOnly cycle, WarEffortType typeId, long amount)
        {
            Cycle = cycle;
            TypeId = typeId;
            Amount = amount;
        }
    }
}
