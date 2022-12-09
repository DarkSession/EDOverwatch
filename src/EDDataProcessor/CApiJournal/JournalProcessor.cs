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
                await using IConsumer consumer = await connection.CreateConsumerAsync("Commander.CApi", RoutingType.Anycast, cancellationToken);
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
            if (commander == null || !commander.CanProcessCApiJournal)
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
            if (profile?.Commander == null)
            {
                return;
            }
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
                            JObject journalObject = JObject.Parse(journalLine);
                            if (journalObject.TryGetValue("event", out JToken? eventJToken) &&
                                eventJToken.Value<string>() is string eventName &&
                                !string.IsNullOrEmpty(eventName) &&
                                JournalEvents.TryGetValue(eventName, out Type? eventClass) &&
                                journalObject.ToObject(eventClass) is JournalEvent journalEvent)
                            {
                                if (journalEvent.BypassLiveStatusCheck || commander.IsInLiveVersion)
                                {
                                    await journalEvent.ProcessEvent(commander, dbContext, activeMqProducer, activeMqTransaction, cancellationToken);
                                }
                            }
                            if (journalObject.TryGetValue("timestamp", out JToken? timestampToken) &&
                                timestampToken.Value<DateTime>() is DateTime timestamp)
                            {
                                commander.JournalLastActivity = timestamp;
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
                day = day.AddDays(1);
                await Task.WhenAll(Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken), dbContext.SaveChangesAsync(cancellationToken));
            }
            while (day <= today);

            commander.JournalLastProcessed = DateTimeOffset.Now;
            if (oAuthCredentials.Status == OAuthCredentialsStatus.Refreshed)
            {
                commander.OAuthAccessToken = oAuthCredentials.AccessToken;
                commander.OAuthRefreshToken = oAuthCredentials.RefreshToken;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
