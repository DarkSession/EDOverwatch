using System.Runtime.Serialization;

namespace EDDataProcessor.EDDN
{
    [EDDNSchema("https://eddn.edcd.io/schemas/journal/1")]
    public class JournalV1 : IEDDNEvent
    {
        [JsonProperty("$schemaRef", Required = Required.Always)]
        public string SchemaRef { get; set; } = string.Empty;

        [JsonProperty("header", Required = Required.Always)]
        public Header Header { get; set; } = new();

        [JsonProperty("message", Required = Required.Always)]
        public JournalV1Message Message { get; set; } = new();

        public async ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer)
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
                            return;
                        }
                        bool isNew = false;
                        StarSystem? starSystem = await dbContext.StarSystems
                                                                .Include(s => s.Allegiance)
                                                                .SingleOrDefaultAsync(m => m.SystemAddress == Message.SystemAddress);
                        if (starSystem == null)
                        {
                            isNew = true;
                            starSystem = new(0,
                                Message.SystemAddress,
                                Message.Name,
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
                            bool changed = false;
                            starSystem.Updated = Message.Timestamp;
                            if (starSystem.Name != Message.Name)
                            {
                                starSystem.Name = Message.Name;
                                changed = true;
                            }
                            if (!string.IsNullOrEmpty(Message.SystemAllegiance))
                            {
                                FactionAllegiance allegiance = await FactionAllegiance.GetByName(Message.SystemAllegiance, dbContext);
                                if (starSystem.Allegiance?.Id != allegiance.Id)
                                {
                                    starSystem.Allegiance = allegiance;
                                    changed = true;
                                }
                            }
                            await dbContext.SaveChangesAsync();
                            if (changed)
                            {
                                await activeMqProducer.SendAsync("StarSystem.Updated", new(JsonConvert.SerializeObject(new StarSystemUpdated(Message.SystemAddress))));
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
                            return;
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
        /*
        [JsonProperty("DistFromStarLS")]
        public decimal DistFromStarLS { get; set; }

        [JsonProperty("StationName")]
        public string StationName { get; set; }

        [JsonProperty("StationType")]
        public string StationType { get; set; }

        [JsonProperty("StationEconomy")]
        public string StationEconomy { get; set; }

        [JsonProperty("StationEconomies")]
        public ICollection<DockedStationEconomy> StationEconomies { get; set; }

        [JsonProperty("StationGovernment")]
        public string StationGovernment { get; set; }

        [JsonProperty("StationServices")]
        public ICollection<string> StationServices { get; set; }

        [JsonProperty("StationFaction")]
        public DockedStationFaction StationFaction { get; set; }

        [JsonProperty("LandingPads")]
        public DockedLandingPads LandingPads { get; set; }

        [JsonProperty("BodyID")]
        public short BodyID { get; set; }

        [JsonProperty("BodyName")]
        public string BodyName { get; set; }

        [JsonProperty("StarType")]
        public string StarType { get; set; }

        [JsonProperty("PlanetClass")]
        public string PlanetClass { get; set; }

        [JsonProperty("Signals")]
        public List<SAASignalsFoundSignal> Signals { get; set; }

        [JsonProperty("Rings")]
        public List<ScanBodyRing> Rings { get; set; }

        [JsonProperty("ReserveLevel")]
        public string ReserveLevel { get; set; }

        [JsonProperty("Category")]
        public string Category { get; set; }

        [JsonProperty("SubCategory")]
        public string SubCategory { get; set; }

        [JsonProperty("Region")]
        public string Region { get; set; }
        */
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
