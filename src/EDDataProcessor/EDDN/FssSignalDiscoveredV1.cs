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

        public async ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
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
            List<long> starSystemSignalsUpdated = new();
            foreach (Signals signal in Message.Signals)
            {
                if (await dbContext.StarSystems.FirstOrDefaultAsync(s => s.SystemAddress == Message.SystemAddress, cancellationToken) is not StarSystem starSystem)
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
                                .FirstOrDefaultAsync(s => s.Name == carrierId, cancellationToken);
                            if (station == null)
                            {
                                isNew = true;
                                station = new(0, carrierId, 0, 0, 4, 4, 8, StationState.Normal, signal.Timestamp, signal.Timestamp)
                                {
                                    StarSystem = starSystem,
                                    Type = await StationType.GetFleetCarrier(dbContext, cancellationToken)
                                };
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
                                await dbContext.SaveChangesAsync(cancellationToken);
                                if (changed)
                                {
                                    StationUpdated stationUpdated = new(station.MarketId, Message.SystemAddress);
                                    await activeMqProducer.SendAsync(StationUpdated.QueueName, StationUpdated.Routing, stationUpdated.Message, activeMqTransaction, cancellationToken);
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
                        bool createdUpdated = false;
                        string maelStromName = maelstromMatch.Groups[1].Value;
                        ThargoidMaelstrom? thargoidMaelstrom = await dbContext.ThargoidMaelstroms
                            .Include(t => t.StarSystem)
                            .FirstOrDefaultAsync(t => t.Name == maelStromName, cancellationToken);
                        if (thargoidMaelstrom == null)
                        {
                            thargoidMaelstrom = new(0, maelStromName, 20m, Message.Timestamp)
                            {
                                StarSystem = starSystem,
                            };
                            await dbContext.SaveChangesAsync(cancellationToken);
                            starSystem.Maelstrom = thargoidMaelstrom;
                            await dbContext.SaveChangesAsync(cancellationToken);
                            createdUpdated = true;
                        }
                        else if (thargoidMaelstrom.Updated < Message.Timestamp)
                        {
                            thargoidMaelstrom.Updated = Message.Timestamp;
                            if (thargoidMaelstrom.StarSystem?.Id != starSystem.Id)
                            {
                                thargoidMaelstrom.StarSystem = starSystem;
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            createdUpdated = true;
                        }
                        if (createdUpdated && thargoidMaelstrom != null)
                        {
                            ThargoidMaelstromCreatedUpdated thargoidMaelstromCreatedUpdated = new(thargoidMaelstrom.Id, thargoidMaelstrom.Name);
                            await activeMqProducer.SendAsync(ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing, thargoidMaelstromCreatedUpdated.Message, activeMqTransaction, cancellationToken);
                        }
                    }
                    else if (signalName.StartsWith("$Warzone_TG_"))
                    {
                        type = StarSystemFssSignalType.AXCZ;
                    }
                    else if (signalName == "$USS_NonHumanSignalSource;")
                    {
                        type = StarSystemFssSignalType.ThargoidActivity;
                    }
                }
                bool signalUpdated = false;
                StarSystemFssSignal? starSystemFssSignal = await dbContext.StarSystemFssSignals
                    .FirstOrDefaultAsync(s => s.StarSystem == starSystem && s.Type == type && s.Name == signalName, cancellationToken);
                if (starSystemFssSignal == null)
                {
                    starSystemFssSignal = new(0, signalName, type, Message.Timestamp, Message.Timestamp)
                    {
                        StarSystem = starSystem
                    };
                    dbContext.StarSystemFssSignals.Add(starSystemFssSignal);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    signalUpdated = true;
                }
                else if (starSystemFssSignal.LastSeen < Message.Timestamp)
                {
                    starSystemFssSignal.LastSeen = Message.Timestamp;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    signalUpdated = true;
                }
                if (signalUpdated && !starSystemSignalsUpdated.Contains(starSystem!.SystemAddress))
                {
                    starSystemSignalsUpdated.Add(starSystem.SystemAddress);
                }
            }
            if (starSystemSignalsUpdated.Any())
            {
                StarSystemFssSignalsUpdated starSystemFssSignalsUpdated = new(Message.SystemAddress);
                await activeMqProducer.SendAsync(StarSystemFssSignalsUpdated.QueueName, StarSystemFssSignalsUpdated.Routing, starSystemFssSignalsUpdated.Message, activeMqTransaction, cancellationToken);
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
        public System.DateTimeOffset Timestamp { get; set; }

        [JsonProperty("SignalName", Required = Required.Always)]
        public string SignalName { get; set; } = string.Empty;

        [JsonProperty("IsStation", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public bool IsStation { get; set; }

        [JsonProperty("USSType", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string USSType { get; set; } = string.Empty;

        [JsonProperty("ThreatLevel", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public int ThreatLevel { get; set; }
    }
}
