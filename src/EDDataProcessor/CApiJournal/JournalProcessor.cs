using EDCApi;
using EDDatabase;
using EDDataProcessor.CApiJournal.Events;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EDDataProcessor.CApiJournal
{
    public class JournalProcessor
    {
        private IConfiguration Configuration { get; }
        private IServiceProvider ServiceProvider { get; }
        private ILogger Log { get; }
        private Dictionary<string, Type> JournalEvents { get; } = new();

        public JournalProcessor(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<JournalProcessor> log)
        {
            Configuration = configuration;
            ServiceProvider = serviceProvider;
            Log = log;
            Dictionary<string, Type> capiEvents = new();
            foreach (Type journalEventType in GetType().Assembly
                                    .GetTypes()
                                    .Where(t => t.IsClass && !t.IsAbstract && typeof(JournalEvent).IsAssignableFrom(t)))
            {
                JournalEvents[journalEventType.Name] = journalEventType;
            }
        }

        public async Task ProcessJournals(CancellationToken cancellationToken = default)
        {
            try
            {
                Endpoint activeMqEndpont = Endpoint.Create(
                    Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                    Configuration.GetValue<int>("ActiveMQ:Port"),
                    Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                    Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

                ConnectionFactory connectionFactory = new();
                await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
                await using IConsumer consumer = await connection.CreateConsumerAsync(CommanderCApi.QueueName, CommanderCApi.Routing, cancellationToken);
                await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync(cancellationToken);

                Log.LogInformation("ProcessJournals: Waiting for messages...");
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Message message = await consumer.ReceiveAsync(cancellationToken);
                        await using Transaction transaction = new();
                        await consumer.AcceptAsync(message, transaction, cancellationToken);
                        string jsonString = message.GetBody<string>();
                        CommanderCApi? commanderCApi = JsonConvert.DeserializeObject<CommanderCApi>(jsonString);
                        if (commanderCApi != null)
                        {
                            await ProcessCommanderCApiMessage(commanderCApi, anonymousProducer, transaction, cancellationToken);
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Processing message exception");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "ProcessJournals exception");
            }
        }

        private async Task ProcessCommanderCApiMessage(CommanderCApi commanderCApi, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
            Commander? commander = await dbContext.Commanders
                .Include(c => c.Station)
                .Include(c => c.System)
                .SingleOrDefaultAsync(c => c.FDevCustomerId == commanderCApi.FDevCustomerId, cancellationToken);
            if (commander == null)
            {
                return;
            }
            List<long> warEffortsUpdatedSystemAddresses = new();
            {
                List<CommanderDeferredJournalEvent> commanderDeferredJournalEvents = await dbContext.CommanderDeferredJournalEvents
                     .Where(c => c.Commander == commander && c.Status == CommanderDeferredJournalEventStatus.Pending)
                     .Include(c => c.System)
                     .Take(10)
                     .ToListAsync(cancellationToken);
                foreach (CommanderDeferredJournalEvent commanderDeferredJournalEvent in commanderDeferredJournalEvents)
                {
                    JournalParameters journalParameters = new(true, WarEffortSource.OverwatchCAPI, commander, commanderDeferredJournalEvent.System, activeMqProducer, activeMqTransaction, 0);
                    await ProcessCommanderCApiMessageEvent(journalParameters, commanderDeferredJournalEvent.Journal, dbContext, cancellationToken);
                    if (!journalParameters.DeferRequested)
                    {
                        commanderDeferredJournalEvent.Status = CommanderDeferredJournalEventStatus.Processed;
                    }
                    if (journalParameters.WarEffortsUpdatedSystemAddresses != null)
                    {
                        warEffortsUpdatedSystemAddresses.AddRange(journalParameters.WarEffortsUpdatedSystemAddresses);
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (warEffortsUpdatedSystemAddresses.Any())
            {
                foreach (long systemAddress in warEffortsUpdatedSystemAddresses.Distinct())
                {
                    WarEffortUpdated warEffortUpdated = new(systemAddress, commander.FDevCustomerId);
                    await activeMqProducer.SendAsync(WarEffortUpdated.QueueName, WarEffortUpdated.Routing, warEffortUpdated.Message, activeMqTransaction, cancellationToken);
                }
                warEffortsUpdatedSystemAddresses.Clear();
            }

            if (!commander.CanProcessCApiJournal)
            {
                return;
            }
            CAPI capi = serviceScope.ServiceProvider.GetRequiredService<CAPI>();
            DateOnly journalDay = DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-14).DateTime);
            int journalLastLine = 0;
            if (commander.JournalDay > journalDay)
            {
                journalDay = commander.JournalDay;
                journalLastLine = commander.JournalLastLine;
            }
            OAuthCredentials oAuthCredentials = new(commander.OAuthTokenType, commander.OAuthAccessToken, commander.OAuthRefreshToken);
            Profile? profile = await capi.GetProfile(oAuthCredentials, cancellationToken);
            if (profile?.Commander != null)
            {
                commander.Name = profile.Commander.Name;
                DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime);
                do
                {
                    (bool success, string? journal) = await capi.GetJournal(oAuthCredentials, journalDay, cancellationToken);
                    if (!success)
                    {
                        Log.LogWarning("Unable to get log for {commanderName} for {date}", commander.Name, journalDay);
                        break;
                    }
                    commander.JournalDay = journalDay;
                    commander.JournalLastLine = journalLastLine;

                    if (!string.IsNullOrEmpty(journal))
                    {
                        int currentLine = 0;
                        try
                        {
                            await ProcessCommanderJournal(journal, journalLastLine, commander, dbContext, activeMqProducer, activeMqTransaction, cancellationToken);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error processing journal", e);
                        }
                        commander.JournalLastLine = currentLine;
                        commander.JournalDay = journalDay;
                    }
                    else if (today != journalDay || DateTimeOffset.UtcNow.Hour > 0 || DateTimeOffset.UtcNow.Minute >= 15)
                    {
                        commander.JournalLastLine = 0;
                        commander.JournalDay = journalDay;
                    }
                    if (warEffortsUpdatedSystemAddresses.Any())
                    {
                        foreach (long systemAddress in warEffortsUpdatedSystemAddresses.Distinct())
                        {
                            WarEffortUpdated warEffortUpdated = new(systemAddress, commander.FDevCustomerId);
                            await activeMqProducer.SendAsync(WarEffortUpdated.QueueName, WarEffortUpdated.Routing, warEffortUpdated.Message, activeMqTransaction, cancellationToken);
                        }
                        warEffortsUpdatedSystemAddresses.Clear();
                    }
                    journalDay = journalDay.AddDays(1);
                    journalLastLine = 0;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                while (journalDay <= today);

                if (commander.HasFleetCarrier != CommanderFleetHasFleetCarrier.No)
                {
                    (bool success, FleetCarrier? fleetCarrier) = await capi.GetFleetCarrier(oAuthCredentials, cancellationToken);
                    if (success)
                    {
                        if (fleetCarrier == null)
                        {
                            commander.HasFleetCarrier = CommanderFleetHasFleetCarrier.No;
                        }
                        else if (fleetCarrier.Cargo != null)
                        {
                            commander.HasFleetCarrier = CommanderFleetHasFleetCarrier.Yes;
                            List<CommanderFleetCarrierCargoItem> commanderFleetCarrierCargoItems = await dbContext.CommanderFleetCarrierCargoItems
                                .Where(c => c.Commander == commander)
                                .Include(c => c.Commodity)
                                .Include(c => c.SourceStarSystem)
                                .ToListAsync(cancellationToken);

                            IEnumerable<FleetCarrierCargo> fcCargo = fleetCarrier.Cargo.Where(c => !string.IsNullOrEmpty(c.Commodity) && c.OriginSystem != null);
                            List<StarSystem> sourceSystems;
                            List<Commodity> commodities;
                            {
                                List<string> cargoCommodities = fcCargo.Select(c => c.Commodity).Distinct().ToList()!;
                                commodities = await dbContext.Commodities.Where(c => cargoCommodities.Contains(c.Name)).ToListAsync(cancellationToken);

                                List<long> cargoSystems = fcCargo.Select(c => (long)c.OriginSystem!).Distinct().ToList()!;
                                sourceSystems = await dbContext.StarSystems.Where(s => cargoSystems.Contains(s.SystemAddress)).ToListAsync(cancellationToken);
                            }

                            foreach (FleetCarrierCargo fleetCarrierCargoEntry in fcCargo)
                            {
                                string? commodityName = fleetCarrierCargoEntry.Commodity?.ToLower();
                                if (commanderFleetCarrierCargoItems.FirstOrDefault(c =>
                                    c.Commodity?.Name.ToLower() == commodityName && 
                                    c.SourceStarSystem?.SystemAddress == fleetCarrierCargoEntry.OriginSystem &&
                                    c.Amount == fleetCarrierCargoEntry.Qty) is CommanderFleetCarrierCargoItem commanderFleetCarrierCargoItem)
                                {
                                    commanderFleetCarrierCargoItems.Remove(commanderFleetCarrierCargoItem);
                                    continue;
                                }
                                StarSystem? starSystem = sourceSystems.FirstOrDefault(s => s.SystemAddress == fleetCarrierCargoEntry.OriginSystem);
                                Commodity? commodity = commodities.FirstOrDefault(c => c.Name.ToLower() == commodityName);
                                if (starSystem == null || commodity == null)
                                {
                                    continue;
                                }

                                CommanderFleetCarrierCargoItem newCommanderFleetCarrierCargoItem = new(0, fleetCarrierCargoEntry.Qty)
                                {
                                    SourceStarSystem = starSystem,
                                    Commander = commander,
                                    Commodity = commodity,
                                };
                                dbContext.CommanderFleetCarrierCargoItems.Add(newCommanderFleetCarrierCargoItem);
                            }
                            dbContext.CommanderFleetCarrierCargoItems.RemoveRange(commanderFleetCarrierCargoItems);
                        }
                    }
                }
                commander.JournalLastProcessed = DateTimeOffset.Now;

                CommanderUpdated commanderUpdated = new(commander.FDevCustomerId);
                await activeMqProducer.SendAsync(CommanderUpdated.QueueName, CommanderUpdated.Routing, commanderUpdated.Message, activeMqTransaction, cancellationToken);
            }
            else
            {
                commander.JournalLastProcessed = DateTimeOffset.Now;
            }
            if (oAuthCredentials.Status == OAuthCredentialsStatus.Expired)
            {
                commander.OAuthStatus = CommanderOAuthStatus.Expired;
            }
            else if (oAuthCredentials.Status == OAuthCredentialsStatus.Refreshed)
            {
                commander.OAuthStatus = CommanderOAuthStatus.Active;
                commander.OAuthAccessToken = oAuthCredentials.AccessToken;
                commander.OAuthRefreshToken = oAuthCredentials.RefreshToken;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<JournalProcessResult> ProcessCommanderJournal(string journal, int lineStart, Commander commander, EdDbContext dbContext, IAnonymousProducer? activeMqProducer, Transaction? activeMqTransaction, CancellationToken cancellationToken)
        {
            using StringReader strReader = new(journal);

            int currentLine = 0;
            List<long> warEffortsUpdatedSystemAddresses = new();
            string? journalLine;
            do
            {
                journalLine = await strReader.ReadLineAsync(cancellationToken);
                currentLine++;
                if (currentLine <= lineStart || string.IsNullOrWhiteSpace(journalLine))
                {
                    continue;
                }
                JournalParameters journalParameters = new(false, WarEffortSource.OverwatchCAPI, commander, commander.System, activeMqProducer, activeMqTransaction, currentLine);
                DateTimeOffset? timestamp = await ProcessCommanderCApiMessageEvent(journalParameters, journalLine, dbContext, cancellationToken);
                if (timestamp is DateTimeOffset t)
                {
                    commander.JournalLastActivity = t;
                }
                if (journalParameters.WarEffortsUpdatedSystemAddresses != null)
                {
                    warEffortsUpdatedSystemAddresses.AddRange(journalParameters.WarEffortsUpdatedSystemAddresses);
                }
            }
            while (!string.IsNullOrEmpty(journalLine));

            if (string.IsNullOrWhiteSpace(journalLine)) // The last line very likely is empty
            {
                currentLine--;
            }

            return new JournalProcessResult(currentLine, warEffortsUpdatedSystemAddresses);
        }

        private async Task<DateTimeOffset?> ProcessCommanderCApiMessageEvent(JournalParameters journalParameters, string journalLine, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            JObject journalObject;
            try
            {
                journalObject = JObject.Parse(journalLine);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Error while parsing journal line {journalLine}", journalLine);
                journalParameters.DeferRequested = true;
                return null;
            }

            if (journalObject.TryGetValue("event", out JToken? eventJToken) &&
                eventJToken.Value<string>() is string eventName &&
                !string.IsNullOrEmpty(eventName) &&
                JournalEvents.TryGetValue(eventName, out Type? eventClass) &&
                journalObject.ToObject(eventClass) is JournalEvent journalEvent)
            {
                if (journalParameters.IsDeferred || journalEvent.BypassLiveStatusCheck || journalParameters.Commander.IsInLiveVersion)
                {
                    await journalEvent.ProcessEvent(journalParameters, dbContext, cancellationToken);
                }
            }
            if (journalObject.TryGetValue("timestamp", out JToken? timestampToken) &&
                timestampToken.Value<DateTime>() is DateTime timestamp)
            {
                return timestamp;
            }
            return null;
        }
    }

    public class JournalProcessResult
    {
        public int CurrentLine { get; }
        public List<long> WarEffortsUpdatedSystemAddresses { get; }

        public JournalProcessResult(int currentLine, List<long> warEffortsUpdatedSystemAddresses)
        {
            CurrentLine = currentLine;
            WarEffortsUpdatedSystemAddresses = warEffortsUpdatedSystemAddresses;
        }
    }
}
