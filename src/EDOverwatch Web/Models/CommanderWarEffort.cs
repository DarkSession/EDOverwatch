namespace EDOverwatch_Web.Models
{
    public class CommanderWarEffort
    {
        public DateOnly Date { get; }
        public string Type { get; }
        public string SystemName { get; }
        public long SystemAddress { get; }
        public long Amount { get; }

        public CommanderWarEffort(EDDatabase.WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            SystemName = warEffort.StarSystem?.Name ?? "Unknown";
            SystemAddress = warEffort.StarSystem?.SystemAddress ?? 0;
            Amount = warEffort.Amount;
        }

        public static Task<List<CommanderWarEffort>> Create(EdDbContext dbContext, ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user?.CommanderId is int commanderId)
            {
                return Create(dbContext, commanderId, cancellationToken);
            }
            return Task.FromResult(new List<CommanderWarEffort>());
        }

        public static Task<List<CommanderWarEffort>> Create(EdDbContext dbContext, Commander commander, CancellationToken cancellationToken)
            => Create(dbContext, commander.Id, cancellationToken);

        public static async Task<List<CommanderWarEffort>> Create(EdDbContext dbContext, int commanderId, CancellationToken cancellationToken)
        {
            List<EDDatabase.WarEffort> warEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Include(w => w.StarSystem)
                .Where(w => w.CommanderId == commanderId)
                .ToListAsync(cancellationToken);
            return warEfforts.Select(w => new CommanderWarEffort(w)).ToList();
        }
    }
}
