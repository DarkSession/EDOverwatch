namespace EDOverwatch_Web.Models
{
    public class OverwatchMaelstroms
    {
        public List<OverwatchMaelstromBasic> Maelstroms { get; }

        public OverwatchMaelstroms(List<OverwatchMaelstromBasic> maelstroms)
        {
            Maelstroms = maelstroms;
        }

        public static async Task<OverwatchMaelstroms> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            var maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .Select(t => new
                {
                    Maelstrom = t,
                    SystemsInAlert = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Alert),
                    SystemsInInvasion = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Invasion),
                    SystemsThargoidControlled = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled),
                    SystemsInRecovery = dbContext.StarSystems.Count(s => s.ThargoidLevel!.Maelstrom == t && s.ThargoidLevel!.State == StarSystemThargoidLevelState.Recovery),

                })
                .ToListAsync(cancellationToken);
            List<OverwatchMaelstromBasic> result = maelstroms.Select(m => new OverwatchMaelstromBasic(m.Maelstrom, m.SystemsInAlert, m.SystemsInInvasion, m.SystemsThargoidControlled, m.SystemsInRecovery)).ToList();

            return new OverwatchMaelstroms(result);
        }
    }
}
