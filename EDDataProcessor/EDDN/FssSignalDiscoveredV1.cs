using System.Text.RegularExpressions;

namespace EDDataProcessor.EDDN
{
    [EDDNSchema("https://eddn.edcd.io/schemas/fsssignaldiscovered/1")]
    internal partial class FssSignalDiscoveredV1 : IEDDNEvent
    {
        [JsonProperty("$schemaRef", Required = Required.Always)]
        public string SchemaRef { get; set; } = string.Empty;

        [JsonProperty("header", Required = Required.Always)]
        public Header Header { get; set; } = new();

        [JsonProperty("message", Required = Required.Always)]
        public FssSignalDiscoveredV1Message Message { get; set; } = new();


        [GeneratedRegex("^(.*?) ([A-Z0-9]{3}\\-[A-Z0-9]{3})$", RegexOptions.IgnoreCase)]
        private static partial Regex FleetCarrierRegexGenerator();
        private static Regex FleetCarrierRegex { get; } = FleetCarrierRegexGenerator();

        [GeneratedRegex("^Maelstrom ([A-Z]+)$", RegexOptions.IgnoreCase)]
        private static partial Regex MaelstromRegexGenerator();
        private static Regex MaelstromRegex { get; } = MaelstromRegexGenerator();

        public async ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer)
        {
            if (!Header.IsLive)
            {
                return;
            }
            switch (Header.SoftwareName)
            {
                // Some senders send bad data
                case "EDDiscovery" when Header.SoftwareVersion == "15.1.0.0":
                case "EDDLite" when Header.SoftwareVersion == "2.3.0.0":
                    {
                        return;
                    }
            }
            foreach (Signals signal in Message.Signals)
            {
                if (await dbContext.StarSystems.FirstOrDefaultAsync(s => s.SystemAddress == Message.SystemAddress) is not StarSystem starSystem)
                {
                    continue;
                }
                string signalName = signal.SignalName;
                StarSystemFssSignalType type = StarSystemFssSignalType.Other;
                if (signal.IsStation)
                {
                    Match match = FleetCarrierRegex.Match(signal.SignalName);
                    if (match.Success)
                    {
                        // string carrierName = match.Groups[1].Value;
                        string carrierId = match.Groups[2].Value;
                        signalName = carrierId;
                        type = StarSystemFssSignalType.FleetCarrier;
                        {
                            bool isNew = false;
                            Station? station = await dbContext.Stations
                                .Include(s => s.StarSystem)
                                .FirstOrDefaultAsync(s => s.Name == carrierId);
                            if (station == null)
                            {
                                isNew = true;
                                station = new(0, carrierId, 0, 0, 4, 4, 8, signal.Timestamp, signal.Timestamp);
                                station.StarSystem = starSystem;
                                station.Type = await StationType.GetFleetCarrier(dbContext);
                                dbContext.Stations.Add(station);
                            }
                            if (isNew || station.Updated < signal.Timestamp)
                            {
                                bool changed = isNew;
                                station.Updated = signal.Timestamp;
                                if (station.StarSystem?.Id != starSystem?.Id)
                                {
                                    station.StarSystem = starSystem;
                                    changed = true;
                                }
                                await dbContext.SaveChangesAsync();
                                if (changed)
                                {
                                    await activeMqProducer.SendAsync("StarSystem.Updated", new(JsonConvert.SerializeObject(new StationUpdated(station.MarketId, Message.SystemAddress))));
                                }
                            }
                        }
                    }
                }
                else
                {
                    Match maelstromMatch = MaelstromRegex.Match(signal.SignalName);
                    if (maelstromMatch.Success)
                    {
                        type = StarSystemFssSignalType.Maelstrom;
                        string maelStromName = maelstromMatch.Groups[1].Value;
                        ThargoidMaelstrom? thargoidMaelstrom = await dbContext.ThargoidMaelstroms
                            .Include(t => t.StarSystem)
                            .FirstOrDefaultAsync(t => t.Name == maelStromName);
                        if (thargoidMaelstrom == null)
                        {
                            thargoidMaelstrom = new(0, maelStromName, Message.Timestamp);
                            thargoidMaelstrom.StarSystem = starSystem;
                            await dbContext.SaveChangesAsync();
                            starSystem.Maelstrom = thargoidMaelstrom;
                            await dbContext.SaveChangesAsync();
                        }
                        else if (thargoidMaelstrom.Updated < Message.Timestamp)
                        {
                            thargoidMaelstrom.Updated = Message.Timestamp;
                            if (thargoidMaelstrom.StarSystem?.Id != starSystem.Id)
                            {
                                thargoidMaelstrom.StarSystem = starSystem;
                            }
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    else if (signalName.StartsWith("$Warzone_TG_"))
                    {
                        type = StarSystemFssSignalType.AXCZ;
                    }
                }
                StarSystemFssSignal? starSystemFssSignal = await dbContext.StarSystemFssSignals
                    .FirstOrDefaultAsync(s => s.StarSystem == starSystem && s.Type == type && s.Name == signalName);
                if (starSystemFssSignal == null)
                {
                    starSystemFssSignal = new(0, signalName, type, Message.Timestamp, Message.Timestamp);
                    starSystemFssSignal.StarSystem = starSystem;
                    dbContext.StarSystemFssSignals.Add(starSystemFssSignal);
                    await dbContext.SaveChangesAsync();
                }
                else if (starSystemFssSignal.LastSeen < Message.Timestamp)
                {
                    starSystemFssSignal.LastSeen = Message.Timestamp;
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }

    public partial class FssSignalDiscoveredV1Message
    {
        [JsonProperty("event", Required = Required.Always)]
        public FssSignalDiscoveredV1MessageEvent Event { get; set; }

        /// <summary>
        /// Whether the sending Cmdr has a Horizons pass.
        /// </summary>
        [JsonProperty("horizons", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool Horizons { get; set; }

        /// <summary>
        /// Whether the sending Cmdr has an Odyssey expansion.
        /// </summary>
        [JsonProperty("odyssey", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool Odyssey { get; set; }

        /// <summary>
        /// Duplicate of the first signal's timestamp, for commonality with other schemas.
        /// </summary>
        [JsonProperty("timestamp", Required = Required.Always)]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }

        /// <summary>
        /// Array of FSSSignalDiscovered events
        /// </summary>
        [JsonProperty("signals", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public ICollection<Signals> Signals { get; set; } = new System.Collections.ObjectModel.Collection<Signals>();

        /// <summary>
        /// Should be added by the sender
        /// </summary>
        [JsonProperty("StarSystem", Required = Required.Always)]
        public string StarSystem { get; set; }

        /// <summary>
        /// Should be added by the sender
        /// </summary>
        [JsonProperty("StarPos", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(3)]
        [System.ComponentModel.DataAnnotations.MaxLength(3)]
        public ICollection<double> StarPos { get; set; } = new System.Collections.ObjectModel.Collection<double>();


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.7.2.0 (Newtonsoft.Json v13.0.0.0)")]
    public enum FssSignalDiscoveredV1MessageEvent
    {

        [System.Runtime.Serialization.EnumMember(Value = @"FSSSignalDiscovered")]
        FSSSignalDiscovered = 0,
    }

    /// <summary>
    /// Single FSSSignalDiscovered event
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.7.2.0 (Newtonsoft.Json v13.0.0.0)")]
    public partial class Signals
    {
        [JsonProperty("timestamp", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public System.DateTimeOffset Timestamp { get; set; }

        [JsonProperty("SignalName", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string SignalName { get; set; }

        [JsonProperty("IsStation", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool IsStation { get; set; }

        [JsonProperty("USSType", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string USSType { get; set; }

        [JsonProperty("SpawningState", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string SpawningState { get; set; }

        [JsonProperty("SpawningFaction", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string SpawningFaction { get; set; }

        [JsonProperty("ThreatLevel", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public int ThreatLevel { get; set; }

        [JsonProperty("patternProperties", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public object PatternProperties { get; set; }
    }
}
