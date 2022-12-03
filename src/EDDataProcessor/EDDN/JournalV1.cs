using System.Net.Sockets;
using System.Runtime.Serialization;

namespace EDDataProcessor.EDDN
{
    [EDDNSchema("https://eddn.edcd.io/schemas/journal/1")]
    internal class JournalV1 : IEDDNEvent
    {
        [JsonProperty("$schemaRef", Required = Required.Always)]
        public string SchemaRef { get; set; } = string.Empty;

        [JsonProperty("header", Required = Required.Always)]
        public Header Header { get; set; } = new();

        [JsonProperty("message", Required = Required.Always)]
        public JournalV1Message Message { get; set; } = new();

        public async ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            if (!Header.IsLive)
            {
                return;
            }
            switch (Message.Event)
            {
                case MessageEvent.FSDJump:
                case MessageEvent.CarrierJump:
                case MessageEvent.Location:
                    {
                        List<double> starPos = Message.StarPos?.ToList() ?? new();
                        if (starPos.Count != 3)
                        {
                            break;
                        }
                        bool isNew = false;
                        StarSystem? starSystem = await dbContext.StarSystems
                                                                .Include(s => s.Allegiance)
                                                                .Include(s => s.Security)
                                                                .SingleOrDefaultAsync(m => m.SystemAddress == Message.SystemAddress, cancellationToken);
                        if (starSystem == null)
                        {
                            isNew = true;
                            starSystem = new(0,
                                Message.SystemAddress,
                                Message.StarSystem,
                                (decimal)starPos[0],
                                (decimal)starPos[1],
                                (decimal)starPos[2],
                                Message.Population ?? 0,
                                Message.Timestamp,
                                Message.Timestamp);
                            dbContext.StarSystems.Add(starSystem);
                        }
                        if (starSystem.Updated < Message.Timestamp || isNew)
                        {
                            bool changed = isNew;
                            starSystem.Updated = Message.Timestamp;
                            if (starSystem.Name != Message.StarSystem)
                            {
                                starSystem.Name = Message.StarSystem;
                                changed = true;
                            }
                            if (!string.IsNullOrEmpty(Message.SystemAllegiance))
                            {
                                FactionAllegiance allegiance = await FactionAllegiance.GetByName(Message.SystemAllegiance, dbContext, cancellationToken);
                                if (starSystem.Allegiance?.Id != allegiance.Id)
                                {
                                    starSystem.Allegiance = allegiance;
                                    changed = true;
                                }
                            }
                            if (!string.IsNullOrEmpty(Message.SystemSecurity))
                            {
                                StarSystemSecurity starSystemSecurity = await StarSystemSecurity.GetByName(Message.SystemSecurity, dbContext, cancellationToken);
                                if (starSystem.Security?.Id != starSystemSecurity.Id)
                                {
                                    starSystem.Security = starSystemSecurity;
                                    changed = true;
                                }
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            if (changed)
                            {
                                await activeMqProducer.SendAsync("StarSystem.Updated", RoutingType.Anycast, new(JsonConvert.SerializeObject(new StarSystemUpdated(Message.SystemAddress))), activeMqTransaction, cancellationToken);
                            }
                        }
                        break;
                    }
            }
            switch (Message.Event)
            {
                case MessageEvent.Docked:
                case MessageEvent.CarrierJump:
                case MessageEvent.Location:
                    {
                        if (Message.MarketID == 0 || (Message.Event == MessageEvent.Location && !Message.Docked))
                        {
                            break;
                        }

                        StarSystem? starSystem = await dbContext.StarSystems.SingleOrDefaultAsync(m => m.SystemAddress == Message.SystemAddress, cancellationToken);
                        if (starSystem == null)
                        {
                            break;
                        }
                        bool isNew = false;
                        Station? station = await dbContext.Stations
                            .Include(s => s.Government)
                            .SingleOrDefaultAsync(s => s.MarketId == Message.MarketID, cancellationToken);
                        if (station == null)
                        {
                            isNew = true;
                            station = new(0, Message.StationName, Message.MarketID, Message.DistFromStarLS, Message.LandingPads?.Small ?? 0, Message.LandingPads?.Medium ?? 0, Message.LandingPads?.Large ?? 0, Message.Timestamp, Message.Timestamp)
                            {
                                Type = await StationType.GetByName(Message.StationType, dbContext, cancellationToken)
                            };
                            dbContext.Stations.Add(station);
                        }
                        if (station.Updated < Message.Timestamp || isNew)
                        {
                            bool changed = isNew;
                            station.Updated = Message.Timestamp;
                            if (station.StarSystem?.Id != starSystem.Id)
                            {
                                station.StarSystem = starSystem;
                                changed = true;
                            }
                            if (station.DistanceFromStarLS != Message.DistFromStarLS)
                            {
                                station.DistanceFromStarLS = Message.DistFromStarLS;
                                changed = true;
                            }
                            if (!string.IsNullOrEmpty(Message.StationEconomy))
                            {
                                Economy? economy = await Economy.GetByName(Message.StationEconomy, dbContext, cancellationToken);
                                if (economy != null)
                                {
                                    if (station.PrimaryEconomy?.Id != economy.Id)
                                    {
                                        station.PrimaryEconomy = economy;
                                        changed = true;
                                    }
                                    if ((Message.StationEconomies?.Count ?? 0) > 1)
                                    {
                                        DockedStationEconomy stationSecondaryEconomy = Message.StationEconomies!
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
                            if (!string.IsNullOrEmpty(Message.StationGovernment))
                            {
                                FactionGovernment? government = await FactionGovernment.GetByName(Message.StationGovernment, dbContext, cancellationToken);
                                if (government != null && station.Government?.Id != government.Id)
                                {
                                    station.Government = government;
                                    changed = true;
                                }
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            if (changed)
                            {
                                // await activeMqProducer.SendAsync("Station.Updated", new(JsonConvert.SerializeObject(new StationUpdated(Message.MarketID, Message.SystemAddress))), transaction, cancellationToken);
                            }
                        }
                        break;
                    }
            }
        }
    }

    public class JournalV1Message
    {
        [JsonProperty("timestamp", Required = Required.Always)]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("event", Required = Required.Always)]
        public MessageEvent Event { get; set; }

        /// <summary>Whether the sending Cmdr has a Horizons pass.</summary>
        [JsonProperty("horizons", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool Horizons { get; set; }

        /// <summary>Whether the sending Cmdr has an Odyssey expansion.</summary>
        [JsonProperty("odyssey", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool Odyssey { get; set; }

        /// <summary>Must be added by the sender if not present in the journal event</summary>
        [JsonProperty("StarSystem", Required = Required.Always)]
        public string StarSystem { get; set; } = string.Empty;

        /// <summary>Must be added by the sender if not present in the journal event</summary>
        [JsonProperty("StarPos", Required = Required.Always)]
        public ICollection<double> StarPos { get; set; } = new System.Collections.ObjectModel.Collection<double>();

        /// <summary>Should be added by the sender if not present in the journal event</summary>
        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("Population")]
        public long? Population { get; set; }

        [JsonProperty("SystemGovernment")]
        public string SystemGovernment { get; set; } = string.Empty;

        [JsonProperty("SystemAllegiance")]
        public string SystemAllegiance { get; set; } = string.Empty;

        [JsonProperty("SystemSecurity")]
        public string SystemSecurity { get; set; } = string.Empty;

        [JsonProperty("SystemEconomy")]
        public string SystemEconomy { get; set; } = string.Empty;

        [JsonProperty("MarketID")]
        public long MarketID { get; set; }

        [JsonProperty("Docked")]
        public bool Docked { get; set; }

        [JsonProperty("DistFromStarLS")]
        public decimal DistFromStarLS { get; set; }

        [JsonProperty("StationName")]
        public string StationName { get; set; } = string.Empty;

        [JsonProperty("StationType")]
        public string StationType { get; set; } = string.Empty;

        [JsonProperty("StationEconomy")]
        public string StationEconomy { get; set; } = string.Empty;

        [JsonProperty("StationEconomies")]
        public ICollection<DockedStationEconomy>? StationEconomies { get; set; }

        [JsonProperty("StationGovernment")]
        public string StationGovernment { get; set; } = string.Empty;

        [JsonProperty("StationFaction")]
        public DockedStationFaction? StationFaction { get; set; }

        [JsonProperty("LandingPads")]
        public DockedLandingPads? LandingPads { get; set; }
    }

    public class DockedLandingPads
    {
        [JsonProperty("Small")]
        public short Small { get; set; }

        [JsonProperty("Medium")]
        public short Medium { get; set; }

        [JsonProperty("Large")]
        public short Large { get; set; }
    }

    public class DockedStationEconomy
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("Proportion")]
        public decimal Proportion { get; set; }
    }

    public class DockedStationFaction
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;
    }

    public enum MessageEvent
    {
        [EnumMember(Value = @"Docked")]
        Docked = 0,

        [EnumMember(Value = @"FSDJump")]
        FSDJump = 1,

        [EnumMember(Value = @"Scan")]
        Scan = 2,

        [EnumMember(Value = @"Location")]
        Location = 3,

        [EnumMember(Value = @"SAASignalsFound")]
        SAASignalsFound = 4,

        [EnumMember(Value = @"CarrierJump")]
        CarrierJump = 5,

        [EnumMember(Value = @"CodexEntry")]
        CodexEntry = 6,
    }
}
