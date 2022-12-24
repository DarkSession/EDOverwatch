using EDCApi;
using EDDataProcessor.CApiJournal.Events;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EDDataProcessor.CApiJournal
{
    internal class JournalProcessor
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
                    JournalParameters journalParameters = new(true, WarEffortSource.OverwatchCAPI, commander, commanderDeferredJournalEvent.System, activeMqProducer, activeMqTransaction);
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
            DateOnly day = DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-14).DateTime);
            int lineStart = 0;
            if (commander.JournalDay > day)
            {
                day = commander.JournalDay;
                lineStart = commander.JournalLastLine + 1;
            }
            OAuthCredentials oAuthCredentials = new(commander.OAuthTokenType, commander.OAuthAccessToken, commander.OAuthRefreshToken);
            Profile? profile = await capi.GetProfile(oAuthCredentials, cancellationToken);
            if (profile?.Commander != null)
            {
                commander.Name = profile.Commander.Name;
                DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime);
                do
                {
                    (bool success, string? journal) = await capi.GetJournal(oAuthCredentials, day, cancellationToken);
                    if (!success)
                    {
                        Log.LogWarning("Unable to get log for {commanderName} for {date}", commander.Name, day);
                        break;
                    }
                    commander.JournalDay = day;

                    if (!string.IsNullOrEmpty(journal))
                    {
                        int line = 0;
                        try
                        {
                            using StringReader strReader = new(journal);
                            string? journalLine;
                            do
                            {
                                journalLine = await strReader.ReadLineAsync(cancellationToken);
                                line++;
                                if (line < lineStart || string.IsNullOrEmpty(journalLine))
                                {
                                    continue;
                                }
                                JournalParameters journalParameters = new(false, WarEffortSource.OverwatchCAPI, commander, commander.System, activeMqProducer, activeMqTransaction);
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
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error processing journal", e);
                        }
                        commander.JournalLastLine = line;
                        commander.JournalDay = day;
                    }
                    else if (today != day || (DateTimeOffset.UtcNow - commander.JournalLastActivity).TotalMinutes > 120)
                    {
                        commander.JournalLastLine = 0;
                        commander.JournalDay = day;
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
                    day = day.AddDays(1);
                    await Task.WhenAll(Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken), dbContext.SaveChangesAsync(cancellationToken));
                }
                while (day <= today);

                commander.JournalLastProcessed = DateTimeOffset.Now;
            }
            if (oAuthCredentials.Status == OAuthCredentialsStatus.Expired)
            {
                commander.OAuthStatus = CommanderOAuthStatus.Expires;
            }
            else if (oAuthCredentials.Status == OAuthCredentialsStatus.Refreshed)
            {
                commander.OAuthStatus = CommanderOAuthStatus.Active;
                commander.OAuthAccessToken = oAuthCredentials.AccessToken;
                commander.OAuthRefreshToken = oAuthCredentials.RefreshToken;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
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
}
