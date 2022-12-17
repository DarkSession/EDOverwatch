global using Discord;
global using EDDatabase;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
using ActiveMQ.Artemis.Client;
using DCoHTrackerDiscordBot.Module;
using Discord.Interactions;
using Discord.WebSocket;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DCoHTrackerDiscordBot
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }
        private static DiscordSocketConfig SocketConfig { get; } = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
        };
        private static InteractionServiceConfig InteractionServiceConfig { get; } = new()
        {
            UseCompiledLambda = true,
        };
        internal static ILogger? Log { get; set; }

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
                .AddDbContext<EdDbContext>()
                .AddSingleton(SocketConfig)
                .AddSingleton(InteractionServiceConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .BuildServiceProvider();

            Log = Services.GetRequiredService<ILogger<Program>>();

            DiscordSocketClient client = Services.GetRequiredService<DiscordSocketClient>();
            client.Log += Events.Log.LogAsync;

            await Services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            {
                await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                await TrackingModule.UpdateSystems(dbContext);
                await TrackingModule.UpdateMaelstroms(dbContext);
            }

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, Configuration.GetValue<string>("Discord:Token"));
            await client.StartAsync();

            _ = ProcessEvents();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        private static async Task ProcessEvents()
        {
            Endpoint activeMqEndpont = Endpoint.Create(
                Configuration!.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                Configuration!.GetValue<int>("ActiveMQ:Port"),
                Configuration!.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                Configuration!.GetValue<string>("ActiveMQ:Password") ?? string.Empty);
            ConnectionFactory connectionFactory = new();
            await using ActiveMQ.Artemis.Client.IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont);

            await using IConsumer consumer = await connection.CreateConsumerAsync(StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing);
            while (true)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync();
                    await consumer.AcceptAsync(message);

                    await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    string jsonString = message.GetBody<string>();
                    StarSystemThargoidLevelChanged? starSystemThargoidLevelChanged = JsonConvert.DeserializeObject<StarSystemThargoidLevelChanged>(jsonString);
                    if (starSystemThargoidLevelChanged != null)
                    {
                        await TrackingModule.UpdateSystems(dbContext);
                    }
                }
                catch (Exception e)
                {
                    Log!.LogError(e, "Event listener exception");
                }
            }
        }

        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}