using EDDataProcessor.EDDN;
using System.Text.RegularExpressions;

namespace EDDataProcessor.CApiJournal.Events.Travel
{
    internal partial class Docked : JournalEvent
    {
        public long MarketID { get; set; }
        public long SystemAddress { get; set; }
        public string StationType { get; set; }
        public string StationName { get; set; }
        public decimal DistFromStarLS { get; set; }
        public string StationEconomy { get; set; }
        public string StationGovernment { get; set; }
        public DockedStationFaction? StationFaction { get; set; }
        public DockedLandingPads? LandingPads { get; set; }
        public string? StationState { get; set; }
        public ICollection<DockedStationEconomy>? StationEconomies { get; set; }

        public Docked(long marketID, long systemAddress, string stationType, string stationName, decimal distFromStarLS, string stationEconomy, string stationGovernment, DockedStationFaction stationFaction, DockedLandingPads? landingPads, string? stationState)
        {
            MarketID = marketID;
            SystemAddress = systemAddress;
            StationType = stationType;
            StationName = stationName;
            DistFromStarLS = distFromStarLS;
            StationEconomy = stationEconomy;
            StationGovernment = stationGovernment;
            StationFaction = stationFaction;
            LandingPads = landingPads;
            StationState = stationState;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            StarSystem? starSystem = await dbContext.StarSystems.SingleOrDefaultAsync(m => m.SystemAddress == SystemAddress, cancellationToken);
            if (starSystem == null)
            {
                return;
            }

            Regex r = FleetCarrierIdRegex();
            bool isFleetCarrier = r.Match(StationName).Success;

            bool isNew = false;
            IQueryable<EDDatabase.Station> stationQuery = dbContext.Stations
                    .Include(s => s.StarSystem)
                    .Include(s => s.Government)
                    .Include(s => s.PrimaryEconomy)
                    .Include(s => s.MinorFaction);
            if (isFleetCarrier)
            {
                stationQuery = stationQuery
                    .Where(s => s.MarketId == MarketID || (s.MarketId == 0 && s.Name == StationName));
            }
            else
            {
                stationQuery = stationQuery
                    .Where(s => s.MarketId == MarketID);
            }
            EDDatabase.Station? station = await stationQuery.FirstOrDefaultAsync(cancellationToken);
            if (station == null)
            {
                isNew = true;
                station = new(0, StationName, MarketID, DistFromStarLS, LandingPads?.Small ?? 0, LandingPads?.Medium ?? 0, LandingPads?.Large ?? 0, EDDatabase.StationState.Normal, RescueShipType.No, Timestamp, Timestamp)
                {
                    Type = await EDDatabase.StationType.GetByName(StationType, dbContext, cancellationToken)
                };
                dbContext.Stations.Add(station);
            }
            if (station.Updated < Timestamp || isNew)
            {
                bool changed = isNew;
                station.Updated = Timestamp;
                if (station.StarSystem?.Id != starSystem.Id)
                {
                    station.StarSystem = starSystem;
                    changed = true;
                }
                if (DistFromStarLS != 0m && station.DistanceFromStarLS != DistFromStarLS)
                {
                    station.DistanceFromStarLS = DistFromStarLS;
                    changed = true;
                }
                if (isFleetCarrier && station.MarketId != MarketID)
                {
                    station.MarketId = MarketID;
                    changed = true;
                }
                if (LandingPads != null)
                {
                    if (station.LandingPadLarge != LandingPads.Large)
                    {
                        station.LandingPadLarge = LandingPads.Large;
                    }
                    if (station.LandingPadMedium != LandingPads.Medium)
                    {
                        station.LandingPadMedium = LandingPads.Medium;
                    }
                    if (station.LandingPadSmall != LandingPads.Small)
                    {
                        station.LandingPadSmall = LandingPads.Small;
                    }
                }
                StationState stationState = EDDatabase.StationState.Normal;
                if (!string.IsNullOrEmpty(StationState) && !Enum.TryParse(StationState, out stationState))
                {
                    Console.WriteLine("Unknown station state: " + StationState);
                }
                if (station.State != stationState)
                {
                    station.State = stationState;
                    changed = true;
                }
                if (!string.IsNullOrEmpty(StationEconomy))
                {
                    Economy? economy = await Economy.GetByName(StationEconomy, dbContext, cancellationToken);
                    if (economy != null)
                    {
                        if (station.PrimaryEconomy?.Id != economy.Id)
                        {
                            station.PrimaryEconomy = economy;
                            changed = true;
                        }
                        if ((StationEconomies?.Count ?? 0) > 1)
                        {
                            DockedStationEconomy stationSecondaryEconomy = StationEconomies!
                                .OrderBy(s => s.Proportion)
                                .Skip(1)
                                .First();
                            Economy? secondaryEconomy = await Economy.GetByName(stationSecondaryEconomy.Name, dbContext, cancellationToken);
                            if (secondaryEconomy != null && station.SecondaryEconomy?.Id != secondaryEconomy.Id)
                            {
                                station.SecondaryEconomy = secondaryEconomy;
                                changed = true;
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(StationGovernment))
                {
                    FactionGovernment? government = await FactionGovernment.GetByName(StationGovernment, dbContext, cancellationToken);
                    if (government != null && station.Government?.Id != government.Id)
                    {
                        station.Government = government;
                        changed = true;
                    }
                }
                if (!string.IsNullOrEmpty(StationFaction?.Name) && StationFaction.Name != station.MinorFaction?.Name)
                {
                    station.MinorFaction = await MinorFaction.GetByName(StationFaction.Name, dbContext, cancellationToken);
                    changed = true;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                /*
                if (changed)
                {
                    StationUpdated stationUpdated = new(MarketID, SystemAddress);
                    await journalParameters.SendMqMessage(StationUpdated.QueueName, StationUpdated.Routing, stationUpdated.Message, cancellationToken);
                }
                */
            }
            journalParameters.Commander.System = starSystem;
            journalParameters.Commander.Station = station;
        }

        [GeneratedRegex("^([A-Z]{3})-([A-Z]{3})$", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex FleetCarrierIdRegex();
    }
}