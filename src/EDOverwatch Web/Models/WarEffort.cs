namespace EDOverwatch_Web.Models
{
    public static class WarEffort
    {
        public static DateOnly GetWarEffortFocusStartDate()
        {
            DateTimeOffset time = WeeklyTick.GetLastTick();
            if (DateTimeOffset.UtcNow.AddDays(-2) > time)
            {
                time = DateTimeOffset.UtcNow.AddDays(-2);
            }
            return DateOnly.FromDateTime(time.DateTime);
        }

        public static async Task<Dictionary<WarEffortTypeGroup, long>> GetTotalWarEfforts(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            DateOnly startDate = GetWarEffortFocusStartDate();
            var totalEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Where(w =>
                        w.Date >= startDate &&
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
            foreach (var total in totalEfforts)
            {
                if (total.Amount == 0)
                {
                    continue;
                }
                if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(total.Type, out WarEffortTypeGroup group))
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

        public static decimal CalculateSystemFocus(IEnumerable<WarEffortTypeSum> systemEfforts, Dictionary<WarEffortTypeGroup, long> totalEffortSums)
        {
            if (systemEfforts.Any())
            {
                Dictionary<WarEffortTypeGroup, long> systemEffortSums = new();
                foreach (var systemEffort in systemEfforts)
                {
                    if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(systemEffort.Type, out WarEffortTypeGroup group))
                    {
                        if (!systemEffortSums.ContainsKey(group))
                        {
                            systemEffortSums[group] = systemEffort.Amount;
                            continue;
                        }
                        systemEffortSums[group] += systemEffort.Amount;
                    }
                }
                if (systemEffortSums.Any())
                {
                    decimal effortFocus = 0;
                    foreach (KeyValuePair<WarEffortTypeGroup, long> effort in totalEffortSums)
                    {
                        if (systemEffortSums.TryGetValue(effort.Key, out long amount) && amount > 0)
                        {
                            effortFocus += ((decimal)amount / (decimal)effort.Value / (decimal)totalEffortSums.Count);
                        }
                    }
                    return Math.Round(effortFocus, 2);
                }
            }
            return 0;
        }
    }

    public class WarEffortTypeSum
    {
        public WarEffortType Type { get; }
        public long Amount { get; }
        public WarEffortTypeSum(WarEffortType type, long amount)
        {
            Type = type;
            Amount = amount;
        }
    }
}
