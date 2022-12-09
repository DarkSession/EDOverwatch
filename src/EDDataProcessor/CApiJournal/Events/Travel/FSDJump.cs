﻿namespace EDDataProcessor.CApiJournal.Events.Travel
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

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            bool isNew = false;
            StarSystem? starSystem = await dbContext.StarSystems
                                                    .Include(s => s.Allegiance)
                                                    .Include(s => s.Security)
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
                    Population ?? 0,
                    false,
                    Timestamp,
                    Timestamp);
                starSystem.WarRelevantSystem = starSystem.IsWarRelevantSystem;
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
                await dbContext.SaveChangesAsync(cancellationToken);
                if (changed)
                {
                    await activeMqProducer.SendAsync("StarSystem.Updated", RoutingType.Anycast, new(JsonConvert.SerializeObject(new StarSystemUpdated(SystemAddress))), activeMqTransaction, cancellationToken);
                }
            }
            journalParameters.Commander.System = starSystem;
        }
    }
}
