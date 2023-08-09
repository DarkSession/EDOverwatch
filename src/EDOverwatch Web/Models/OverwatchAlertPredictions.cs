namespace EDOverwatch_Web.Models
{
    public class OverwatchAlertPredictions
    {
        public OverwatchThargoidCycle Cycle { get; }
        public List<OverwatchAlertPredictionMaelstrom> Maelstroms { get; set; }

        public OverwatchAlertPredictions(ThargoidCycle thargoidCycle, List<ThargoidMaelstrom> maelstroms, List<AlertPrediction> alertPredictions)
        {
            Cycle = new(thargoidCycle);
            Maelstroms = new();
            foreach (ThargoidMaelstrom maelstrom in maelstroms)
            {
                List<AlertPrediction> systems = alertPredictions
                    .Where(a => a.Maelstrom?.Id == maelstrom.Id)
                    .ToList();
                OverwatchAlertPredictionMaelstrom maelstromEntry = new(maelstrom, systems);
                Maelstroms.Add(maelstromEntry);
            }
        }

        public static async Task<OverwatchAlertPredictions> Create(EdDbContext dbContext, CancellationToken cancellationToken)
        {
            ThargoidCycle nextThargoidCycle = await dbContext.GetThargoidCycle(DateTimeOffset.UtcNow, cancellationToken, 1);

            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            List<AlertPrediction> alertPredictions = await dbContext.AlertPredictions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Attackers!)
                .ThenInclude(a => a.StarSystem!.ThargoidLevel)
                .Where(a => a.Cycle == nextThargoidCycle)
                .ToListAsync(cancellationToken);

            return new OverwatchAlertPredictions(nextThargoidCycle, maelstroms, alertPredictions);
        }
    }
}
