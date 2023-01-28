namespace EDOverwatch_Web.Models
{
    public class CommanderWarEfforts
    {
        public List<CommanderWarEffort> WarEfforts { get; }
        public List<string> WarEffortTypeGroups => Enum.GetValues<WarEffortTypeGroup>()
            .Select(w => w.GetEnumMemberValue())
            .ToList();
        public List<OverwatchThargoidCycle> RecentThargoidCycles { get; }

        protected CommanderWarEfforts(List<EDDatabase.WarEffort> warEfforts, List<OverwatchThargoidCycle> recentThargoidCycles)
        {
            WarEfforts = warEfforts.Select(w => new CommanderWarEffort(w)).ToList();
            RecentThargoidCycles = recentThargoidCycles;
        }

        public static async Task<CommanderWarEfforts?> Create(EdDbContext dbContext, ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user?.CommanderId is int commanderId)
            {
                return await Create(dbContext, commanderId, cancellationToken);
            }
            return null;
        }

        public static Task<CommanderWarEfforts> Create(EdDbContext dbContext, Commander commander, CancellationToken cancellationToken)
            => Create(dbContext, commander.Id, cancellationToken);

        public static async Task<CommanderWarEfforts> Create(EdDbContext dbContext, int commanderId, CancellationToken cancellationToken)
        {
            List<EDDatabase.WarEffort> warEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Include(w => w.StarSystem)
                .Include(w => w.Cycle)
                .Where(w => w.CommanderId == commanderId)
                .ToListAsync(cancellationToken);

            List<OverwatchThargoidCycle> recentThargoidCycles = await OverwatchThargoidCycle.GetRecentThargoidCycles(dbContext, cancellationToken);
            CommanderWarEfforts result = new(warEfforts, recentThargoidCycles);
            return result;
        }
    }

    public class CommanderWarEffort
    {
        public DateOnly Date { get; }
        public string Type { get; }
        public string TypeGroup { get; }
        public string SystemName { get; }
        public long SystemAddress { get; }
        public long Amount { get; }
        public DateOnly Cycle { get; }

        public CommanderWarEffort(EDDatabase.WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            SystemName = warEffort.StarSystem?.Name ?? "Unknown";
            SystemAddress = warEffort.StarSystem?.SystemAddress ?? 0;
            Amount = warEffort.Amount;
            Cycle = warEffort.Cycle?.Cycle ?? default;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(warEffort.Type, out WarEffortTypeGroup group))
            {
                TypeGroup = group.GetEnumMemberValue();
            }
            else
            {
                TypeGroup = string.Empty;
            }
        }
    }
}
