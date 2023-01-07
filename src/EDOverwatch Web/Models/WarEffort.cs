namespace EDOverwatch_Web.Models
{
    public static class WarEffort
    {
        public static async Task<Dictionary<WarEffortTypeGroup, long>> GetTotalWarEfforts(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            var totalEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w =>
                        w.Date >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) &&
                        w.StarSystem!.WarRelevantSystem &&
                        w.Side == WarEffortSide.Humans)
                .GroupBy(w => new
                {
                    w.Type,
                })
                .Select(w => new
                {
                    w.Key.Type,
                    Amount = w.Sum(g => g.Amount),
                })
                .ToListAsync(cancellationToken);

            Dictionary<WarEffortTypeGroup, long> totalEffortSums = new();
            foreach (var total in totalEfforts.GroupBy(e => e.Type).Select(e => new
            {
                e.Key,
                Amount = e.Sum(g => g.Amount),
            }))
            {
                if (total.Amount == 0)
                {
                    continue;
                }
                if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(total.Key, out WarEffortTypeGroup group))
                {
                    if (!totalEffortSums.ContainsKey(group))
                    {
                        totalEffortSums[group] = total.Amount;
                        continue;
                    }
                    totalEffortSums[group] += total.Amount;
                }
            }
            return totalEffortSums;
        }
    }
}
