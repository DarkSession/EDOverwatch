using System.Text.RegularExpressions;

namespace EDDataProcessor.CApiJournal.Events.Exploration
{
    internal partial class FSSSignalDiscovered : JournalEvent
    {
        public long SystemAddress { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string SignalName { get; set; }
        public bool? IsStation { get; set; }
        public string? USSType { get; set; }

        public FSSSignalDiscovered(long systemAddress, string signalName, bool? isStation, string? uSSType)
        {
            SystemAddress = systemAddress;
            SignalName = signalName;
            IsStation = isStation;
            USSType = uSSType;
        }

        public override async ValueTask ProcessEvent(JournalParameters journalParameters, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.StarSystems.FirstOrDefaultAsync(s => s.SystemAddress == SystemAddress, cancellationToken) is not StarSystem starSystem)
            {
                return;
            }
            List<long> starSystemSignalsUpdated = new();

            StarSystemFssSignalType type = StarSystemFssSignalType.Other;
            if (IsStation is bool isStation)
            {
                Match match = FleetCarrierRegex.Match(SignalName);
                if (match.Success)
                {
                    // string carrierName = match.Groups[1].Value;
                    string carrierId = match.Groups[2].Value;
                    SignalName = carrierId;
                    type = StarSystemFssSignalType.FleetCarrier;
                    {
                        bool isNew = false;
                        EDDatabase.Station? station = await dbContext.Stations
                            .Include(s => s.StarSystem)
                            .FirstOrDefaultAsync(s => s.Name == carrierId, cancellationToken);
                        if (station == null)
                        {
                            isNew = true;
                            station = new(0, carrierId, 0, 0, 4, 4, 8, StationState.Normal, RescueShipType.No, Timestamp, Timestamp)
                            {
                                StarSystem = starSystem,
                                Type = await StationType.GetFleetCarrier(dbContext, cancellationToken)
                            };
                            dbContext.Stations.Add(station);
                        }
                        if (isNew || station.Updated < Timestamp)
                        {
                            bool changed = isNew;
                            station.Updated = Timestamp;
                            if (station.StarSystem?.Id != starSystem?.Id)
                            {
                                station.StarSystem = starSystem;
                                changed = true;
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                            if (changed)
                            {
                                StationUpdated stationUpdated = new(station.MarketId, SystemAddress);
                                await journalParameters.SendMqMessage(StationUpdated.QueueName, StationUpdated.Routing, stationUpdated.Message, cancellationToken);
                            }
                        }
                    }
                }
            }
            else
            {
                Match maelstromMatch = MaelstromRegex.Match(SignalName);
                if (maelstromMatch.Success)
                {
                    type = StarSystemFssSignalType.Maelstrom;
                    bool createdUpdated = false;
                    string maelStromName = maelstromMatch.Groups[1].Value;
                    ThargoidMaelstrom? thargoidMaelstrom = await dbContext.ThargoidMaelstroms
                        .Include(t => t.StarSystem)
                        .ThenInclude(s => s!.ThargoidLevel)
                        .ThenInclude(t => t!.Maelstrom)
                        .FirstOrDefaultAsync(t => t.Name == maelStromName, cancellationToken);
                    if (thargoidMaelstrom == null)
                    {
                        thargoidMaelstrom = new(0, maelStromName, 20m, 0, Timestamp)
                        {
                            StarSystem = starSystem,
                        };
                        await dbContext.SaveChangesAsync(cancellationToken);
                        createdUpdated = true;
                    }
                    else if (thargoidMaelstrom.Updated < Timestamp)
                    {
                        thargoidMaelstrom.Updated = Timestamp;
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
                        await journalParameters.SendMqMessage(ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing, thargoidMaelstromCreatedUpdated.Message, cancellationToken);
                    }
                }
                else if (SignalName.StartsWith("$Warzone_TG_"))
                {
                    type = StarSystemFssSignalType.AXCZ;
                }
                else if (SignalName == "$USS_NonHumanSignalSource;")
                {
                    type = StarSystemFssSignalType.ThargoidActivity;
                }
            }
            bool signalUpdated = false;
            StarSystemFssSignal? starSystemFssSignal = await dbContext.StarSystemFssSignals
                .FirstOrDefaultAsync(s => s.StarSystem == starSystem && s.Type == type && s.Name == SignalName, cancellationToken);
            if (starSystemFssSignal == null)
            {
                starSystemFssSignal = new(0, SignalName, type, Timestamp, Timestamp)
                {
                    StarSystem = starSystem
                };
                dbContext.StarSystemFssSignals.Add(starSystemFssSignal);
                await dbContext.SaveChangesAsync(cancellationToken);
                signalUpdated = true;
            }
            else if (starSystemFssSignal.LastSeen < Timestamp)
            {
                starSystemFssSignal.LastSeen = Timestamp;
                await dbContext.SaveChangesAsync(cancellationToken);
                signalUpdated = true;
            }
            if (signalUpdated && !starSystemSignalsUpdated.Contains(starSystem!.SystemAddress))
            {
                starSystemSignalsUpdated.Add(starSystem.SystemAddress);
            }

            if (starSystemSignalsUpdated.Any())
            {
                StarSystemFssSignalsUpdated starSystemFssSignalsUpdated = new(SystemAddress);
                await journalParameters.SendMqMessage(StarSystemFssSignalsUpdated.QueueName, StarSystemFssSignalsUpdated.Routing, starSystemFssSignalsUpdated.Message, cancellationToken);
            }
        }

        [GeneratedRegex("^(.*?) ([A-Z0-9]{3}\\-[A-Z0-9]{3})$", RegexOptions.IgnoreCase)]
        private static partial Regex FleetCarrierRegexGenerator();
        private static Regex FleetCarrierRegex { get; } = FleetCarrierRegexGenerator();

        [GeneratedRegex("^Maelstrom ([A-Z]+)$", RegexOptions.IgnoreCase)]
        private static partial Regex MaelstromRegexGenerator();
        private static Regex MaelstromRegex { get; } = MaelstromRegexGenerator();
    }
}
