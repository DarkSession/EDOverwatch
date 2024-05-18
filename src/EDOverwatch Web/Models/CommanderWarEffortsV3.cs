using System.Text.Json.Serialization;

namespace EDOverwatch_Web.Models
{
    public class CommanderWarEffortsV3
    {
        public List<CommandWarEffortCycle> CycleWarEfforts { get; }
        public List<string> WarEffortTypeGroups => Enum.GetValues<WarEffortTypeGroup>()
            .Select(w => w.GetEnumMemberValue())
            .ToList();
        public List<OverwatchThargoidCycle> ThargoidCycles { get; }

        protected CommanderWarEffortsV3(List<EDDatabase.WarEffort> warEfforts, List<OverwatchThargoidCycle> thargoidCycles)
        {
            CycleWarEfforts = thargoidCycles
                .Select(t => new CommandWarEffortCycle(t, warEfforts.Where(w => w.CycleId == t.Id)))
                .ToList();
            ThargoidCycles = thargoidCycles;
        }

        public static async Task<CommanderWarEffortsV3?> Create(EdDbContext dbContext, ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user?.CommanderId is int commanderId)
            {
                return await Create(dbContext, commanderId, cancellationToken);
            }
            return null;
        }

        public static Task<CommanderWarEffortsV3> Create(EdDbContext dbContext, Commander commander, CancellationToken cancellationToken)
            => Create(dbContext, commander.Id, cancellationToken);

        public static async Task<CommanderWarEffortsV3> Create(EdDbContext dbContext, int commanderId, CancellationToken cancellationToken)
        {
            List<EDDatabase.WarEffort> warEfforts = await dbContext.WarEfforts
                .AsNoTracking()
                .Include(w => w.StarSystem)
                .Include(w => w.StarSystem!.ThargoidLevel)
                .Include(w => w.StarSystem!.ThargoidLevel!.Maelstrom)
                .Include(w => w.Cycle)
                .Where(w => w.CommanderId == commanderId && w.StarSystem!.ThargoidLevel != null)
                .OrderBy(w => w.Cycle!.Start)
                .ToListAsync(cancellationToken);

            List<OverwatchThargoidCycle> thargoidCycles = await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken);
            CommanderWarEffortsV3 result = new(warEfforts, thargoidCycles);
            return result;
        }
    }

    public class CommandWarEffortCycle
    {
        public OverwatchThargoidCycle ThargoidCycle { get; }
        public List<CommanderWarEffortCycleStarSystem> StarSystems { get; }

        public CommandWarEffortCycle(OverwatchThargoidCycle thargoidCycle, IEnumerable<EDDatabase.WarEffort> cycleWarEfforts)
        {
            ThargoidCycle = thargoidCycle;
#pragma warning disable IDE0305 // Simplify collection initialization
            StarSystems = cycleWarEfforts
                .GroupBy(c => c.StarSystemId)
                .Select(c => new CommanderWarEffortCycleStarSystem(c.First().StarSystem!, c))
                .OrderByDescending(c => c.WarEfforts.Max(w => w.Date))
                .ThenByDescending(c => c.WarEfforts.Max(w => w.Id))
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
        }
    }

    public class CommanderWarEffortCycleStarSystem
    {
        public OverwatchStarSystem StarSystem { get; }
        public List<CommanderWarEffortCycleStarSystemWarEffort> WarEfforts { get; }

        public CommanderWarEffortCycleStarSystem(StarSystem starSystem, IEnumerable<EDDatabase.WarEffort> cycleStarSystemWarEfforts)
        {
            StarSystem = new(starSystem, false);
            WarEfforts = cycleStarSystemWarEfforts
                .Select(w => new CommanderWarEffortCycleStarSystemWarEffort(w))
                .ToList();
        }
    }

    public class CommanderWarEffortCycleStarSystemWarEffort
    {
        [JsonIgnore]
        public int Id { get; }
        public DateOnly Date { get; }
        public string Type { get; }
        public WarEffortTypeGroup Group { get; }
        public long Amount { get; }

        public CommanderWarEffortCycleStarSystemWarEffort(EDDatabase.WarEffort warEffort)
        {
            Id = warEffort.Id;
            Date = warEffort.Date;
            Type = warEffort.Type.GetEnumMemberValue();
            Amount = warEffort.Amount;
            if (EDDatabase.WarEffort.WarEffortGroups.TryGetValue(warEffort.Type, out WarEffortTypeGroup group))
            {
                Group = group;
            }
        }
    }
}
