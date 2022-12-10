using EDUtils;

namespace EDOverwatch_Web.Models
{
    public class CommanderWarEffort
    {
        public DateOnly Date { get; set; }
        public string Type { get; set; }
        public string SystemName { get; set; }
        public long Amount { get; set; }

        public CommanderWarEffort(WarEffort warEffort)
        {
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            SystemName = warEffort.StarSystem?.Name ?? "Unknown";
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
            List<WarEffort> warEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Include(w => w.StarSystem)
                .Where(w => w.CommanderId == commanderId)
                .ToListAsync(cancellationToken);
            return warEfforts.Select(w => new CommanderWarEffort(w)).ToList();
        }
    }
}
