using Newtonsoft.Json.Linq;
using System.Reflection;

namespace EDDataProcessor.EDDN
{
    internal class EDDNProcessor
    {
        private ILogger Log { get; }
        private IServiceProvider ServiceProvider { get; }
        private Dictionary<string, Type> EDDNProcessors { get; } = [];

        public EDDNProcessor(ILogger<EDDNProcessor> log, IServiceProvider serviceProvider)
        {
            Log = log;
            ServiceProvider = serviceProvider;
            foreach (Type type in typeof(Program).Assembly.GetTypes().Where(i => typeof(IEDDNEvent).IsAssignableFrom(i)))
            {
                if (type.GetCustomAttributes(typeof(EDDNSchemaAttribute)).FirstOrDefault() is EDDNSchemaAttribute eddnSchemaAttribute)
                {
                    EDDNProcessors[eddnSchemaAttribute.SchemaUrl] = type;
                }
            }
        }

        public async Task ProcessMessages(IConnection connection, CancellationToken cancellationToken = default)
        {
            Log.LogInformation("Starting EDDNProcessor");

            try
            {
                await using IConsumer consumer = await connection.CreateConsumerAsync("EDDN", RoutingType.Anycast, cancellationToken);
                await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync(cancellationToken);

                Log.LogInformation("EDDNProcessor: Waiting for messages...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Log.LogInformation("EDDNProcessor: Waiting for message...");
                        Message message = await consumer.ReceiveAsync(cancellationToken);
                        Log.LogInformation("EDDNProcessor: Message received");
                        await using Transaction transaction = new();
                        await consumer.AcceptAsync(message, transaction, cancellationToken);
                        Log.LogInformation("EDDNProcessor: Message accepted");
                        string jsonString = message.GetBody<string>();
                        JObject json = JObject.Parse(jsonString);
                        if (json.TryGetValue("$schemaRef", out JToken? schemaRef) &&
                            schemaRef.Value<string>() is string schema &&
                            !string.IsNullOrEmpty(schema) &&
                            EDDNProcessors.TryGetValue(schema, out Type? schemaClass) &&
                            json.ToObject(schemaClass) is IEDDNEvent eddnEvent)
                        {
                            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                            Log.LogInformation("EDDNProcessor: Processing EDDN message");
                            await eddnEvent.ProcessEvent(dbContext, anonymousProducer, transaction, cancellationToken);
                        }
                        else
                        {
                            Log.LogWarning("EDDNProcessor: Received EDDN message with an invalid/known schema.");
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "EDDNProcessor: Processing message exception");
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }

                Log.LogInformation("EDDNProcessor: End of while.");
            }
            catch (Exception e)
            {
                Log.LogError(e, "EDDNProcessor exception");
            }
        }
    }
}
