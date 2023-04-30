using EDCApi;
using EDDataProcessor.Journal.Events;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDataProcessor.Journal
{
    public class PublicApiJournalProcessor
    {
        private IConfiguration Configuration { get; }
        private IServiceProvider ServiceProvider { get; }
        private ILogger Log { get; }
        private Dictionary<string, Type> JournalEvents { get; } = new();

        public PublicApiJournalProcessor(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<CApiJournalProcessor> log)
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
                await using IConsumer consumer = await connection.CreateConsumerAsync(CommanderPublicApiEvents.QueueName, CommanderPublicApiEvents.Routing, cancellationToken);
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
                        CommanderPublicApiEvents? commanderPublicApiEvents = JsonConvert.DeserializeObject<CommanderPublicApiEvents>(jsonString);
                        if (commanderPublicApiEvents != null)
                        {
                            await ProcessMessage(commanderPublicApiEvents, anonymousProducer, transaction, cancellationToken);
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Processing message exception");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "ProcessJournals exception");
            }
        }

        private async Task ProcessMessage(CommanderPublicApiEvents commanderPublicApiEvents, IAnonymousProducer activeMqProducer, Transaction activeMqTransaction, CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
            Commander? commander = await dbContext.Commanders
                .Include(c => c.Station)
                .Include(c => c.System)
                .SingleOrDefaultAsync(c => c.Id == commanderPublicApiEvents.CommanderId, cancellationToken);
            if (commander == null || commanderPublicApiEvents.Events == null)
            {
                return;
            }
            List<long> warEffortsUpdatedSystemAddresses = new();

           foreach (JObject eventData in commanderPublicApiEvents.Events)
            {
                PublicApiEventBase? journalEvent = eventData.ToObject<PublicApiEventBase>();
                if (journalEvent == null)
                {
                    continue;
                }
                if (journalEvent.CMDR != commander.Name)
                {
                    Log.LogWarning("ProcessMessage: Journal event has commander {journalCommander} while commander {commanderName} is expected", journalEvent.CMDR, commander.Name);
                    continue;
                }
                StarSystem? starSystem = await dbContext.StarSystems.SingleOrDefaultAsync(s => s.SystemAddress == journalEvent.SystemAddress, cancellationToken);
                if (starSystem == null)
                {
                    Log.LogWarning("ProcessMessage: System with address {systemAddress} not found", journalEvent.SystemAddress);
                    continue;
                }
                JournalParameters journalParameters = new(false, WarEffortSource.OverwatchPublicAPI, commander, starSystem, activeMqProducer, activeMqTransaction, 0);
                await ProcessCommanderCApiMessageEvent(journalParameters, eventData, dbContext, cancellationToken);
            }
        }

        private async Task ProcessCommanderCApiMessageEvent(JournalParameters journalParameters, JObject journalObject, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (journalObject.TryGetValue("event", out JToken? eventJToken) &&
                eventJToken.Value<string>() is string eventName &&
                !string.IsNullOrEmpty(eventName) &&
                JournalEvents.TryGetValue(eventName, out Type? eventClass) &&
                journalObject.ToObject(eventClass) is JournalEvent journalEvent)
            {
                await journalEvent.ProcessEvent(journalParameters, dbContext, cancellationToken);
            }
        }
    }
}
