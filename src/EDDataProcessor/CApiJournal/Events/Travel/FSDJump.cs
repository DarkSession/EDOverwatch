using EDDataProcessor.EDDN;
using EDDataProcessor.Journal;

namespace EDDataProcessor.CApiJournal.Events.Travel
{
    internal class FSDJump : JournalEvent
    {
        public long? Population { get; set; }
        public string StarSystem { get; set; }
        public long SystemAddress { get; set; }
        public List<double> StarPos { get; set; }
        public string? SystemGovernment { get; set; }
        public string? SystemAllegiance { get; set; }
        public string? SystemEconomy { get; set; }
        public string? SystemSecurity { get; set; }
        public List<FSDJumpFaction>? Factions { get; set; }
        public FSDJumpThargoidWar? ThargoidWar { get; set; }

        public FSDJump(long? population, string starSystem, long systemAddress, List<double> starPos, string? systemGovernment, string? systemAllegiance, string? systemEconomy, string? systemSecurity)
        {
            Population = population;
            StarSystem = starSystem;
            SystemAddress = systemAddress;
            StarPos = starPos;
            SystemGovernment = systemGovernment;
            SystemAllegiance = systemAllegiance;
            SystemEconomy = systemEconomy;
            SystemSecurity = systemSecurity;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            long population = Population ?? 0;
            bool isNew = false;
            StarSystem? starSystem = await dbContext.StarSystems
                                                    .Include(s => s.Allegiance)
                                                    .Include(s => s.Security)
                                                    .Include(s => s.ThargoidLevel)
                                                    .Include(s => s.ThargoidLevel!.CurrentProgress)
                                                    .Include(s => s.ThargoidLevel!.StateExpires)
                                                    .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                                                    .Include(s => s.MinorFactionPresences!)
                                                    .ThenInclude(m => m.MinorFaction)
                                                    .SingleOrDefaultAsync(m => m.SystemAddress == SystemAddress, cancellationToken);
            if (starSystem == null)
            {
                isNew = true;
                starSystem = new(0,
                    SystemAddress,
                    StarSystem,
                    (decimal)StarPos[0],
                    (decimal)StarPos[1],
                    (decimal)StarPos[2],
                    population,
                    population,
                    population,
                    false,
                    false,
                    Timestamp,
                    Timestamp)
                {
                    MinorFactionPresences = new(),
                };
                starSystem.UpdateWarRelevantSystem();
                dbContext.StarSystems.Add(starSystem);
            }
            if (starSystem.Updated < Timestamp || isNew)
            {
                bool changed = isNew;
                starSystem.Updated = Timestamp;
                if (starSystem.Name != StarSystem)
                {
                    starSystem.Name = StarSystem;
                    changed = true;
                }
                if (!string.IsNullOrEmpty(SystemAllegiance))
                {
                    FactionAllegiance allegiance = await FactionAllegiance.GetByName(SystemAllegiance, dbContext, cancellationToken);
                    if (starSystem.Allegiance?.Id != allegiance.Id)
                    {
                        starSystem.Allegiance = allegiance;
                        changed = true;
                    }
                }
                if (!string.IsNullOrEmpty(SystemSecurity))
                {
                    StarSystemSecurity starSystemSecurity = await StarSystemSecurity.GetByName(SystemSecurity, dbContext, cancellationToken);
                    if (starSystem.Security?.Id != starSystemSecurity.Id)
                    {
                        starSystem.Security = starSystemSecurity;
                        changed = true;
                    }
                }
                if (starSystem.Population != population &&
                    starSystem.ThargoidLevel?.State != StarSystemThargoidLevelState.Controlled &&
                    !(starSystem.ThargoidLevel?.State == StarSystemThargoidLevelState.Invasion && starSystem.Population < population))
                {
                    starSystem.Population = population;
                    changed = true;
                }
                if (starSystem.OriginalPopulation < population)
                {
                    starSystem.OriginalPopulation = population;
                    changed = true;
                }
                if (starSystem.PopulationMin > population)
                {
                    starSystem.PopulationMin = population;
                    changed = true;
                }
                if (Factions?.Any() ?? false)
                {
                    foreach (FSDJumpFaction faction in Factions.Where(f => !string.IsNullOrEmpty(f.Allegiance)))
                    {
                        MinorFaction minorFaction = await MinorFaction.GetByName(faction.Name, dbContext, cancellationToken);
                        if (minorFaction.Allegiance?.Name != faction.Allegiance)
                        {
                            minorFaction.Allegiance = await FactionAllegiance.GetByName(faction.Allegiance, dbContext, cancellationToken);
                            changed = true;
                        }
                        if (!starSystem.MinorFactionPresences!.Any(m => m.MinorFaction == minorFaction))
                        {
                            starSystem.MinorFactionPresences!.Add(new(0)
                            {
                                MinorFaction = minorFaction,
                                // StarSystem = starSystem,
                            });
                            changed = true;
                        }
                    }
                    if (starSystem.MinorFactionPresences!.RemoveAll(m => !Factions.Any(f => f.Name == m.MinorFaction?.Name)) > 0)
                    {
                        changed = true;
                    }
                }
                if (ThargoidWar != null)
                {
                    changed = await starSystem.UpdateThargoidWar(Timestamp, ThargoidWar, dbContext, cancellationToken) || changed;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                if (changed)
                {
                    StarSystemUpdated starSystemUpdated = new(SystemAddress);
                    await journalParameters.SendMqMessage(StarSystemUpdated.QueueName, StarSystemUpdated.Routing, starSystemUpdated.Message, cancellationToken);
                }
            }
            journalParameters.Commander.System = starSystem;
        }
    }
}
