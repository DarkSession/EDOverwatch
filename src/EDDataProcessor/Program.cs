global using ActiveMQ.Artemis.Client;
global using ActiveMQ.Artemis.Client.Transactions;
global using EDDatabase;
global using Messages;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Newtonsoft.Json;
using EDCApi;
using EDDataProcessor.CApiJournal;
using EDDataProcessor.EDDN;
using EDDataProcessor.Inara;

namespace EDDataProcessor
{
    internal class Program
    {
        public static IConfiguration? Configuration { get; private set; }
        public static IServiceProvider? Services { get; private set; }

        static async Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("edcapi.json")
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
                .AddSingleton<EDDNProcessor>()
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
                    optionsBuilder.UseProjectables();
                })
                .AddScoped<CAPI>()
                .AddScoped<FDevOAuth>()
                .AddSingleton<JournalProcessor>()
                .AddSingleton<InaraClient>()
                .BuildServiceProvider();

            CancellationToken cancellationToken = default;

            Endpoint activeMqEndpont = Endpoint.Create(
               Configuration!.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
               Configuration!.GetValue<int?>("ActiveMQ:Port") ?? throw new Exception("No ActiveMQ port configured"),
               Configuration!.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
               Configuration!.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);

            Task eddnProcessorTask = Task.CompletedTask;
            if (Configuration.GetValue<bool>("EDDN:Enabled"))
            {
                EDDNProcessor eddnProcessor = Services.GetRequiredService<EDDNProcessor>();
                eddnProcessorTask = eddnProcessor.ProcessMessages(connection, cancellationToken);
            }
            Task scheduledInaraAndIdaUpdates = Task.CompletedTask;
            if (Configuration.GetValue<bool>("Inara:Enabled"))
            {
                scheduledInaraAndIdaUpdates = ScheduledInaraAndIdaUpdates(connection, cancellationToken);
            }
            Task journalProcessorTask = Task.CompletedTask;
            Task journalCommanderSelectionTask = Task.CompletedTask;
            if (Configuration.GetValue<bool>("EDCApi:Enabled"))
            {
                JournalProcessor journalProcessor = Services.GetRequiredService<JournalProcessor>();
                journalProcessorTask = journalProcessor.ProcessJournals(cancellationToken);
                journalCommanderSelectionTask = CommanderJournalUpdateQueue(connection, cancellationToken);
            }
            await Task.WhenAll(eddnProcessorTask, scheduledInaraAndIdaUpdates, journalProcessorTask, journalCommanderSelectionTask);
        }

        private static async Task ScheduledInaraAndIdaUpdates(IConnection connection, CancellationToken cancellationToken)
        {
            await using IProducer warEffortUpdatedProducer = await connection.CreateProducerAsync(WarEffortUpdated.QueueName, WarEffortUpdated.Routing, cancellationToken);

            ILogger log = Services!.GetRequiredService<ILogger<Program>>();
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Configuration!.GetValue<bool>("Inara:Enabled"))
                {
                    try
                    {
                        await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                        if (ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, typeof(UpdateFromInara)) is UpdateFromInara updateFromInara)
                        {
                            log.LogInformation("Inara: Starting update");
                            List<long> systemsUpdated = await updateFromInara.Update().ToListAsync(cancellationToken);
                            log.LogInformation("Inara: Updated {systemsCount} systems", systemsUpdated.Count);
                            if (systemsUpdated.Any())
                            {
                                foreach (long systemAddress in systemsUpdated.Distinct())
                                {
                                    WarEffortUpdated warEffortUpdated = new(systemAddress, null);
                                    await warEffortUpdatedProducer.SendAsync(warEffortUpdated.Message, cancellationToken);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError(e, "Inara update exception");
                    }
                }
                Random rnd = new();
                int d = rnd.Next(4, 6);
                await Task.Delay(TimeSpan.FromHours(d), cancellationToken);
            }
        }

        private static async Task CommanderJournalUpdateQueue(IConnection connection, CancellationToken cancellationToken)
        {
            await using IProducer commanderCApiProducer = await connection.CreateProducerAsync(CommanderCApi.QueueName, CommanderCApi.Routing, cancellationToken);
            ILogger log = Services!.GetRequiredService<ILogger<Program>>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    List<Commander> commanders = await dbContext.Commanders
                        .AsNoTracking()
                        .Where(c =>
                            c.OAuthStatus == CommanderOAuthStatus.Active &&
                            (c.JournalLastProcessed < DateTimeOffset.UtcNow.AddMinutes(-60) ||
                            (c.JournalLastProcessed < DateTimeOffset.UtcNow.AddMinutes(-30) && c.JournalLastActivity > DateTimeOffset.UtcNow.AddHours(-2))))
                        .ToListAsync(cancellationToken);
                    foreach (Commander commander in commanders)
                    {
                        CommanderCApi commanderCApi = new(commander.FDevCustomerId);
                        await commanderCApiProducer.SendAsync(commanderCApi.Message, cancellationToken);
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (Exception e)
                {
                    log.LogError(e, "Commander journal selection update exception");
                }
            }
        }
    }
}