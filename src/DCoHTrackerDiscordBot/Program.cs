global using Discord;
global using EDDatabase;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
using ActiveMQ.Artemis.Client;
using DCoHTrackerDiscordBot.Module;
using Discord.Interactions;
using Discord.WebSocket;
using EDUtils;
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
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
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

            Endpoint activeMqEndpont = Endpoint.Create(
                Configuration!.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                Configuration!.GetValue<int>("ActiveMQ:Port"),
                Configuration!.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                Configuration!.GetValue<string>("ActiveMQ:Password") ?? string.Empty);
            ConnectionFactory connectionFactory = new();
            await using ActiveMQ.Artemis.Client.IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont);
            await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync();

            Services = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(Configuration.GetSection("Logging"));
                })
                .AddDbContext<EdDbContext>(optionsBuilder =>
                {
                    string connectionString = Configuration.GetValue<string>("ConnectionString") ?? string.Empty;
                    optionsBuilder.UseMySql(connectionString,
                        new MariaDbServerVersion(new Version(10, 3, 25)),
                        options =>
                        {
                            options.EnableRetryOnFailure();
                            options.CommandTimeout(60 * 10 * 1000);
                        })
#if DEBUG
                        .EnableSensitiveDataLogging()
                        .LogTo(Console.WriteLine)
#endif
                        ;
                })
                .AddSingleton(SocketConfig)
                .AddSingleton(InteractionServiceConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(connection)
                .AddSingleton(anonymousProducer)
                .AddScoped<MessagesHandler>()
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
                await TrackingModule.UpdateTitans(dbContext);
            }

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, Configuration.GetValue<string>("Discord:Token"));
            await client.StartAsync();

            TaskCompletionSource t = new();
            client.Ready += async () =>
            {
                t.TrySetResult();
                await client.SetGameAsync("the galaxy", type: ActivityType.Watching);
            };

            await t.Task;
            _ = ProcessStarSystemThargoidLevelChangedEvents(connection);
            _ = ProcessStarSystemUpdateQueueItemUpdatedEvents(connection);

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        private static async Task ProcessStarSystemThargoidLevelChangedEvents(ActiveMQ.Artemis.Client.IConnection connection)
        {
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

        private static async Task ProcessStarSystemUpdateQueueItemUpdatedEvents(ActiveMQ.Artemis.Client.IConnection connection)
        {
            await using IConsumer consumer = await connection.CreateConsumerAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing);
            while (true)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync();
                    await consumer.AcceptAsync(message);

                    await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    DiscordSocketClient client = serviceScope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
                    string jsonString = message.GetBody<string>();
                    StarSystemUpdateQueueItemUpdated? starSystemUpdateQueueItemUpdated = JsonConvert.DeserializeObject<StarSystemUpdateQueueItemUpdated>(jsonString);
                    if (starSystemUpdateQueueItemUpdated != null &&
                        await dbContext.StarSystemUpdateQueueItems
                            .Include(s => s.StarSystem)
                            .ThenInclude(s => s!.ThargoidLevel)
                            .ThenInclude(t => t!.Maelstrom)
                            .SingleOrDefaultAsync(s => s.Id == starSystemUpdateQueueItemUpdated.Id) is StarSystemUpdateQueueItem starSystemUpdateQueueItem &&
                        starSystemUpdateQueueItem.Status == StarSystemUpdateQueueItemStatus.PendingNotification &&
                        starSystemUpdateQueueItem.StarSystem != null)
                    {
                        try
                        {
                            var channel = await client.GetChannelAsync(starSystemUpdateQueueItem.DiscordChannelId);
                            var u = await client.GetUserAsync(starSystemUpdateQueueItem.DiscordUserId);
                            if (channel is ITextChannel textChannel &&
                                u is IUser user)
                            {
                                AllowedMentions mentions = new()
                                {
                                    AllowedTypes = AllowedMentionTypes.Users,
                                };

                                string text = $"{user.Mention}, your system update request for **{Format.Sanitize(starSystemUpdateQueueItem.StarSystem.Name)}** has been ";
                                text += starSystemUpdateQueueItem.ResultBy switch
                                {
                                    StarSystemUpdateQueueItemResultBy.Manual => "manually reviewed",
                                    _ => "automatically reviewed by Overwatch",
                                };
                                text += ".\r\n";
                                text += starSystemUpdateQueueItem.Result switch
                                {
                                    StarSystemUpdateQueueItemResult.NotUpdated => "**No** changes have been made as at the time of the review we believe all data was updated and correct.",
                                    _ => "The system has been updated to reflect the in-game status and data.",
                                };
                                text += "\r\n";
                                if (starSystemUpdateQueueItem.ResultBy == StarSystemUpdateQueueItemResultBy.Automatic)
                                {
                                    text += "If you believe that the data is still not updated or wrong, please submit another request and it will undergo a manual review.\r\n";
                                }
                                text += "Thank you for your contribution in keeping all data correct and updated.";

                                EmbedBuilder embed = new EmbedBuilder()
                                    .WithTitle("System Update Request")
                                    .WithDescription(text);

                                embed.AddField("System", starSystemUpdateQueueItem.StarSystem.Name, true);
                                if (!string.IsNullOrEmpty(starSystemUpdateQueueItem.StarSystem.ThargoidLevel?.Maelstrom?.Name))
                                {
                                    embed.AddField("Maelstrom", starSystemUpdateQueueItem.StarSystem.ThargoidLevel.Maelstrom.Name, true);
                                }
                                string systemState = starSystemUpdateQueueItem.StarSystem.ThargoidLevel?.State.GetEnumMemberValue() ?? "Clear";
                                embed.AddField("System State", systemState, true);
                                if (starSystemUpdateQueueItem.StarSystem.ThargoidLevel?.Progress != null)
                                {
                                    embed.AddField("Progress", starSystemUpdateQueueItem.StarSystem.ThargoidLevel.Progress + " %", true);
                                }
                                await textChannel.SendMessageAsync($"{user.Mention}", embed: embed.Build(), allowedMentions: mentions);
                            }
                            else
                            {
                                Log!.LogWarning("StarSystemUpdateQueueItemUpdated {id}: Unable to send message. Channel or user not found.", starSystemUpdateQueueItem.Id);
                            }
                        }
                        catch (Exception e)
                        {
                            Log!.LogError(e, "Error while processing StarSystemUpdateQueueItemUpdated event");
                        }
                        starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.Completed;
                        await dbContext.SaveChangesAsync();
                        await Task.Delay(TimeSpan.FromSeconds(1));
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