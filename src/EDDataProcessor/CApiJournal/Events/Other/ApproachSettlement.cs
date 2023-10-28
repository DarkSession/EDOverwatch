namespace EDDataProcessor.CApiJournal.Events.Other
{
    internal class ApproachSettlement : JournalEvent
    {
        public string Name { get; set; }
        public string? StationEconomy { get; set; }
        public long SystemAddress { get; set; }
        public long MarketID { get; set; }
        public string BodyName { get; set; }
        public int BodyID { get; set; }

        public ApproachSettlement(string name, string? stationEconomy, long systemAddress, long marketID, string bodyName, int bodyID)
        {
            Name = name;
            StationEconomy = stationEconomy;
            SystemAddress = systemAddress;
            MarketID = marketID;
            BodyName = bodyName;
            BodyID = bodyID;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(StationEconomy))
            {
                return;
            }
            Economy? economy = await Economy.GetByName(StationEconomy, dbContext, cancellationToken);
            EDDatabase.Station? station = await dbContext.Stations
                .Include(s => s.Body)
                .Include(s => s.StarSystem)
                .Include(s => s.PrimaryEconomy)
                .FirstOrDefaultAsync(s => s.StarSystem!.SystemAddress == SystemAddress && s.MarketId == MarketID, cancellationToken);
            if (station == null)
            {
                StarSystem? starSystem = await dbContext.StarSystems.SingleOrDefaultAsync(m => m.SystemAddress == SystemAddress, cancellationToken);
                if (starSystem == null)
                {
                    return;
                }

                station = new(0, Name ?? string.Empty, MarketID, 0, 0, 0, 0, StationState.Normal, RescueShipType.No, Timestamp, Timestamp)
                {
                    PrimaryEconomy = economy,
                    StarSystem = starSystem,
                    Type = await StationType.GetByName(StationType.OdysseySettlementType, dbContext, cancellationToken),
                };
                dbContext.Stations.Add(station);
            }
            else if (economy != null && station.PrimaryEconomy?.Id != economy?.Id)
            {
                station.PrimaryEconomy = economy;
            }

            if (station.Body?.Name != BodyName)
            {
                StarSystemBody? systemBody = await dbContext.StarSystemBodies
                    .FirstOrDefaultAsync(s => s.StarSystem == station.StarSystem && s.Name == BodyName, cancellationToken);
                if (systemBody == null)
                {
                    systemBody = new(0, BodyID, BodyName, null, false, null)
                    {
                        StarSystem = station.StarSystem,
                    };
                    dbContext.StarSystemBodies.Add(systemBody);
                }
                station.Body = systemBody;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
