using System.Runtime.Serialization;

namespace EDDataProcessor.EDDN
{
    [EDDNSchema("https://eddn.edcd.io/schemas/approachsettlement/1")]
    internal partial class ApproachSettlementV1 : IEDDNEvent
    {
        [JsonProperty("$schemaRef", Required = Required.Always)]
        public string SchemaRef { get; set; } = string.Empty;

        [JsonProperty("header", Required = Required.Always)]
        public Header Header { get; set; } = new Header();

        [JsonProperty("message", Required = Required.Always)]
        public ApproachSettlementV1Message Message { get; set; } = new ApproachSettlementV1Message();

        public async ValueTask ProcessEvent(EdDbContext dbContext, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            if (!Header.IsLive || string.IsNullOrEmpty(Message.BodyName))
            {
                return;
            }
            if (Message.Event == ApproachSettlementMessageEvent.ApproachSettlement)
            {
                if (string.IsNullOrEmpty(Message.StationEconomy) || Message.MarketID == 0)
                {
                    return;
                }

                Station? station = await dbContext.Stations
                    .Include(s => s.Body)
                    .Include(s => s.StarSystem)
                    .Include(s => s.PrimaryEconomy)
                    .FirstOrDefaultAsync(s => s.StarSystem!.SystemAddress == Message.SystemAddress && s.MarketId == Message.MarketID, cancellationToken);
                if (station != null)
                {
                    Economy? economy = await Economy.GetByName(Message.StationEconomy, dbContext, cancellationToken);
                    if (economy != null && station.PrimaryEconomy?.Id != economy?.Id)
                    {
                        station.PrimaryEconomy = economy;
                    }

                    if (station.Body?.Name != Message.BodyName)
                    {
                        StarSystemBody? systemBody = await dbContext.StarSystemBodies
                            .FirstOrDefaultAsync(s => s.StarSystem == station.StarSystem && s.Name == Message.BodyName, cancellationToken);
                        if (systemBody == null)
                        {
                            systemBody = new(0, Message.BodyID, Message.BodyName, null, false, null)
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
    }

    public partial class ApproachSettlementV1Message
    {
        [JsonProperty("timestamp", Required = Required.Always)]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("event", Required = Required.Always)]
        public ApproachSettlementMessageEvent Event { get; set; }

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
        /// Must be added by the sender
        /// </summary>
        [JsonProperty("StarSystem", Required = Required.Always)]
        public string? StarSystem { get; set; }

        /// <summary>
        /// Must be added by the sender
        /// </summary>
        [JsonProperty("StarPos", Required = Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(3)]
        [System.ComponentModel.DataAnnotations.MaxLength(3)]
        public ICollection<double> StarPos { get; set; } = [];

        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }

        /// <summary>
        /// Name of settlement
        /// </summary>
        [JsonProperty("Name", Required = Required.Always)]
        public string? Name { get; set; }

        [JsonProperty("MarketID", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public long MarketID { get; set; }

        [JsonProperty("StationEconomy")]
        public string? StationEconomy { get; set; }

        [JsonProperty("BodyID", Required = Required.Always)]
        public int BodyID { get; set; }

        [JsonProperty("BodyName", Required = Required.Always)]
        public string? BodyName { get; set; }

        [JsonProperty("Latitude", Required = Required.Always)]
        public double Latitude { get; set; }

        [JsonProperty("Longitude", Required = Required.Always)]
        public double Longitude { get; set; }
    }

    public enum ApproachSettlementMessageEvent
    {
        [EnumMember(Value = @"ApproachSettlement")]
        ApproachSettlement = 0,
    }
}