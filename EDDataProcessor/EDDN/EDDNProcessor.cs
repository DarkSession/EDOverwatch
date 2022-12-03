using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace EDDataProcessor.EDDN
{
    internal class EDDNProcessor
    {
        private IConfiguration Configuration { get; }
        private ILogger Log { get; }
        private IServiceProvider ServiceProvider { get; }
        private Dictionary<string, Type> EDDNProcessors { get; } = new();

        public EDDNProcessor(IConfiguration configuration, ILogger<EDDNProcessor> log, IServiceProvider serviceProvider)
        {
            Configuration = configuration;
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

        public async Task ProcessMessages(CancellationToken cancellationToken = default)
        {
            Endpoint activeMqEndpont = Endpoint.Create(
                Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                Configuration.GetValue<int>("ActiveMQ:Port"),
                Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
            await using IConsumer consumer = await connection.CreateConsumerAsync("EDDN", RoutingType.Anycast, cancellationToken);
            await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync(cancellationToken);
                    await using Transaction transaction = new();
                    await consumer.AcceptAsync(message, transaction, cancellationToken);
                    string jsonString = message.GetBody<string>();
                    JObject json = JObject.Parse(jsonString);
                    if (json.TryGetValue("$schemaRef", out JToken? schemaRef) &&
                        schemaRef.Value<string>() is string schema &&
                        !string.IsNullOrEmpty(schema) &&
                        EDDNProcessors.TryGetValue(schema, out Type? schemaClass) &&
                        json.ToObject(schemaClass) is IEDDNEvent eddnEvent)
                    {
                        using IServiceScope serviceScope = ServiceProvider.CreateScope();
                        EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                        {
                            Log.LogError("Unable to connect to database...");
                            await transaction.RollbackAsync(cancellationToken);
                            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        }
                        await eddnEvent.ProcessEvent(dbContext, anonymousProducer, transaction, cancellationToken);
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Processing message exception");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
    }
}
