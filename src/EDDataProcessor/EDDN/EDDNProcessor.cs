﻿using Microsoft.Extensions.DependencyInjection;
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
            Log.LogInformation("Starting EDDNProcessor");

            try
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

                Log.LogInformation("EDDNProcessor: Waiting for messages...");

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
                            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                            {
                                Log.LogError("Unable to connect to database...");
                                await transaction.RollbackAsync(cancellationToken);
                                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                                continue;
                            }
                            Log.LogDebug("Received EDDN message");
                            await eddnEvent.ProcessEvent(dbContext, anonymousProducer, transaction, cancellationToken);
                        }
                        else
                        {
                            Log.LogWarning("Received EDDN message with an invalid/known schema.");
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
            catch (Exception e)
            {
                Log.LogError(e, "EDDNProcessor exception");
            }
        }
    }
}
