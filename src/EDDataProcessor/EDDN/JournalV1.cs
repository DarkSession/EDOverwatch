using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace EDDataProcessor.EDDN
{
    [EDDNSchema("https://eddn.edcd.io/schemas/journal/1")]
    internal partial class JournalV1 : IEDDNEvent
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
                case JournalMessageEvent.FSDJump:
                case JournalMessageEvent.CarrierJump:
                case JournalMessageEvent.Location:
                    {
                        List<double> starPos = Message.StarPos?.ToList() ?? new();
                        if (starPos.Count != 3)
                        {
                            break;
                        }
                        bool isNew = false;
                        long population = Message.Population ?? 0;
                        StarSystem? starSystem = await dbContext.StarSystems
                                                                .AsSplitQuery()
                                                                .Include(s => s.Allegiance)
                                                                .Include(s => s.Security)
                                                                .Include(s => s.ThargoidLevel)
                                                                .Include(s => s.MinorFactionPresences!)
                                                                .ThenInclude(m => m.MinorFaction)
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
                                population,
                                population,
                                population,
                                false,
                                false,
                                Message.Timestamp,
                                Message.Timestamp)
                            {
                                MinorFactionPresences = new(),
                            };
                            starSystem.UpdateWarRelevantSystem();
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
                            if (Message.Factions?.Any() ?? false)
                            {
                                foreach (FSDJumpFaction faction in Message.Factions.Where(f => !string.IsNullOrEmpty(f.Allegiance)))
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
                                        });
                                        changed = true;
                                    }
                                }
                                if (starSystem.MinorFactionPresences!.RemoveAll(m => !Message.Factions.Any(f => f.Name == m.MinorFaction?.Name)) > 0)
                                {
                                    changed = true;
                                }
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            if (changed)
                            {
                                StarSystemUpdated starSystemUpdated = new(Message.SystemAddress);
                                await activeMqProducer.SendAsync(StarSystemUpdated.QueueName, StarSystemUpdated.Routing, starSystemUpdated.Message, activeMqTransaction, cancellationToken);
                            }
                        }
                        break;
                    }
                case JournalMessageEvent.Scan:
                    {
                        if (Message.SurfaceGravity > 0m)
                        {
                            StarSystemBody? starSystemBody = await dbContext.StarSystemBodies
                                .FirstOrDefaultAsync(s => s.StarSystem!.SystemAddress == Message.SystemAddress && s.BodyId == Message.BodyID, cancellationToken);
                            if (starSystemBody != null)
                            {
                                bool changed = false;
                                if (Message.SurfaceGravity != null &&
                                    starSystemBody.Gravity != Message.SurfaceGravity)
                                {
                                    starSystemBody.Gravity = Message.SurfaceGravity;
                                    changed = true;
                                }
                                if (Message.SurfacePressure != null &&
                                    starSystemBody.SurfacePressure != Message.SurfacePressure)
                                {
                                    starSystemBody.SurfacePressure = Message.SurfacePressure;
                                    changed = true;
                                }
                                if (Message.Atmosphere != null)
                                {
                                    bool hasAtmosphere = !string.IsNullOrEmpty(Message.Atmosphere);
                                    if (starSystemBody.HasAtmosphere != hasAtmosphere)
                                    {
                                        starSystemBody.HasAtmosphere = hasAtmosphere;
                                        changed = true;
                                    }
                                }
                                if (changed)
                                {
                                    await dbContext.SaveChangesAsync(cancellationToken);
                                }
                            }
                        }
                        break;
                    }
            }
            switch (Message.Event)
            {
                case JournalMessageEvent.Docked:
                case JournalMessageEvent.CarrierJump:
                case JournalMessageEvent.Location:
                    {
                        if (Message.MarketID == 0 || Message.Event == JournalMessageEvent.Location && !Message.Docked)
                        {
                            break;
                        }

                        StarSystem? starSystem = await dbContext.StarSystems.SingleOrDefaultAsync(m => m.SystemAddress == Message.SystemAddress, cancellationToken);
                        if (starSystem == null)
                        {
                            break;
                        }

                        Regex r = FleetCarrierIdRegex();
                        bool isFleetCarrier = r.Match(Message.StationName).Success;

                        bool isNew = false;
                        IQueryable<Station> stationQuery = dbContext.Stations
                            .Include(s => s.StarSystem)
                            .Include(s => s.Government)
                            .Include(s => s.PrimaryEconomy)
                            .Include(s => s.MinorFaction);
                        if (isFleetCarrier)
                        {
                            stationQuery = stationQuery
                                .Where(s => s.MarketId == Message.MarketID || (s.MarketId == 0 && s.Name == Message.StationName));
                        }
                        else
                        {
                            stationQuery = stationQuery
                                .Where(s => s.MarketId == Message.MarketID);
                        }
                        Station? station = await stationQuery.FirstOrDefaultAsync(cancellationToken);
                        if (station == null)
                        {
                            isNew = true;
                            station = new(0, Message.StationName, Message.MarketID, Message.DistFromStarLS, Message.LandingPads?.Small ?? 0, Message.LandingPads?.Medium ?? 0, Message.LandingPads?.Large ?? 0, StationState.Normal, RescueShipType.No, Message.Timestamp, Message.Timestamp)
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
                            if (Message.DistFromStarLS != 0m && station.DistanceFromStarLS != Message.DistFromStarLS)
                            {
                                station.DistanceFromStarLS = Message.DistFromStarLS;
                                changed = true;
                            }
                            if (isFleetCarrier && station.MarketId != Message.MarketID)
                            {
                                station.MarketId = Message.MarketID;
                                changed = true;
                            }
                            if (Message.Event != JournalMessageEvent.Location && Message.LandingPads != null)
                            {
                                if (station.LandingPadLarge != Message.LandingPads.Large)
                                {
                                    station.LandingPadLarge = Message.LandingPads.Large;
                                }
                                if (station.LandingPadMedium != Message.LandingPads.Medium)
                                {
                                    station.LandingPadMedium = Message.LandingPads.Medium;
                                }
                                if (station.LandingPadSmall != Message.LandingPads.Small)
                                {
                                    station.LandingPadSmall = Message.LandingPads.Small;
                                }
                            }
                            if (Message.Event != JournalMessageEvent.Location)
                            {
                                StationState stationState = StationState.Normal;
                                if (!string.IsNullOrEmpty(Message.StationState) && !Enum.TryParse(Message.StationState, out stationState))
                                {
                                    Console.WriteLine("Unknown station state: " + Message.StationState);
                                }
                                if (station.State != stationState)
                                {
                                    station.State = stationState;
                                    changed = true;
                                }
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
                            if (!string.IsNullOrEmpty(Message.StationFaction?.Name) && Message.StationFaction.Name != station.MinorFaction?.Name)
                            {
                                station.MinorFaction = await MinorFaction.GetByName(Message.StationFaction.Name, dbContext, cancellationToken);
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            if (changed)
                            {
                                StationUpdated stationUpdated = new(Message.MarketID, Message.SystemAddress);
                                await activeMqProducer.SendAsync(StationUpdated.QueueName, StationUpdated.Routing, stationUpdated.Message, activeMqTransaction, cancellationToken);
                            }
                        }
                        break;
                    }
            }
        }

        [GeneratedRegex("^([A-Z]{3})-([A-Z]{3})$", RegexOptions.IgnoreCase, "en-CH")]
        private static partial Regex FleetCarrierIdRegex();
    }

    public class JournalV1Message
    {
        [JsonProperty("timestamp", Required = Required.Always)]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("event", Required = Required.Always)]
        public JournalMessageEvent Event { get; set; }

        /// <summary>Must be added by the sender if not present in the journal event</summary>
        [JsonProperty("StarSystem", Required = Required.Always)]
        public string StarSystem { get; set; } = string.Empty;

        /// <summary>Must be added by the sender if not present in the journal event</summary>
        [JsonProperty("StarPos", Required = Required.Always)]
        public ICollection<double> StarPos { get; set; } = new System.Collections.ObjectModel.Collection<double>();

        /// <summary>Should be added by the sender if not present in the journal event</summary>
        [JsonProperty("SystemAddress", Required = Required.Always)]
        public long SystemAddress { get; set; }

        [JsonProperty("Population")]
        public long? Population { get; set; }

        [JsonProperty("SystemAllegiance")]
        public string SystemAllegiance { get; set; } = string.Empty;

        [JsonProperty("SystemSecurity")]
        public string SystemSecurity { get; set; } = string.Empty;

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

        [JsonProperty("StationState")]
        public string StationState { get; set; } = string.Empty;

        [JsonProperty("StationEconomies")]
        public ICollection<DockedStationEconomy>? StationEconomies { get; set; }

        [JsonProperty("StationGovernment")]
        public string StationGovernment { get; set; } = string.Empty;

        [JsonProperty("StationFaction")]
        public DockedStationFaction? StationFaction { get; set; }

        [JsonProperty("LandingPads")]
        public DockedLandingPads? LandingPads { get; set; }

        [JsonProperty("BodyName")]
        public string? BodyName { get; set; }

        [JsonProperty("BodyID")]
        public int BodyID { get; set; }

        [JsonProperty("SurfaceGravity")]
        public decimal? SurfaceGravity { get; set; }

        [JsonProperty("Atmosphere")]
        public string? Atmosphere { get; set; }

        [JsonProperty("SurfacePressure")]
        public decimal? SurfacePressure { get; set; }

        [JsonProperty("Factions")]
        public List<FSDJumpFaction>? Factions { get; set; }
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

    public class FSDJumpFaction
    {
        public string Name { get; set; } = string.Empty;
        public string Allegiance { get; set; } = string.Empty;
    }

    public enum JournalMessageEvent
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
