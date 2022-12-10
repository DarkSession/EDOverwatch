using ActiveMQ.Artemis.Client;
using Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestEvents
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
#if DEBUG
                .AddJsonFile("appsettings.dev.json", optional: true)
#endif
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            Services = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(Configuration.GetSection("Logging"));
                })

                .BuildServiceProvider();

            CancellationToken cancellationToken = CancellationToken.None;

            Endpoint activeMqEndpont = Endpoint.Create(
               Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
               Configuration.GetValue<int?>("ActiveMQ:Port") ?? throw new Exception("No ActiveMQ port configured"),
               Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
               Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
            await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                StarSystemUpdated starSystemUpdated = new(1659744799067);
                await anonymousProducer.SendAsync(StarSystemUpdated.QueueName, RoutingType.Anycast, starSystemUpdated.Message, cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                WarEffortUpdated warEffortUpdated = new(1659744799067, 5572229);
                await anonymousProducer.SendAsync(WarEffortUpdated.QueueName, RoutingType.Multicast, warEffortUpdated.Message, cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}