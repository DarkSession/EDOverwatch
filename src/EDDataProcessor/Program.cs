global using ActiveMQ.Artemis.Client;
global using ActiveMQ.Artemis.Client.Transactions;
global using EDDatabase;
global using Messages;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
global using Newtonsoft.Json;
using EDCApi;
using EDDataProcessor.CApiJournal;
using EDDataProcessor.EDDN;
using EDDataProcessor.IDA;
using EDDataProcessor.Inara;
using Microsoft.Extensions.DependencyInjection;

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
                .AddDbContext<EdDbContext>()
                .AddScoped<CAPI>()
                .AddScoped<FDevOAuth>()
                .AddSingleton<JournalProcessor>()
                .AddScoped<IdaClient>()
                .AddSingleton<InaraClient>()
                .BuildServiceProvider();

            CancellationToken cancellationToken = default;

            Task eddnProcessorTask = Task.CompletedTask;
            if (Configuration.GetValue<bool>("EDDN:Enabled"))
            {
                EDDNProcessor eddnProcessor = Services.GetRequiredService<EDDNProcessor>();
                eddnProcessorTask = eddnProcessor.ProcessMessages(cancellationToken);
            }
            Task scheduledUpdates = Task.CompletedTask;
            if (Configuration.GetValue<bool>("Inara:Enabled") || Configuration.GetValue<bool>("IDA:Enabled"))
            {
                scheduledUpdates = ScheduledUpdates(cancellationToken);
            }
            Task journalProcessorTask = Task.CompletedTask;
            Task journalCommanderSelectionTask = Task.CompletedTask;
            if (Configuration.GetValue<bool>("EDCApi:Enabled"))
            {
                JournalProcessor journalProcessor = Services.GetRequiredService<JournalProcessor>();
                journalProcessorTask = journalProcessor.ProcessJournals(cancellationToken);
                journalCommanderSelectionTask = CommanderJournalUpdateQueue(cancellationToken);
            }
            await Task.WhenAll(eddnProcessorTask, scheduledUpdates, journalProcessorTask, journalCommanderSelectionTask);
        }

        private static async Task ScheduledUpdates(CancellationToken cancellationToken)
        {
            ILogger log = Services!.GetRequiredService<ILogger<Program>>();
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Configuration!.GetValue<bool>("IDA:Enabled"))
                {
                    try
                    {
                        await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                        IdaClient idaClient = serviceScope.ServiceProvider.GetRequiredService<IdaClient>();
                        await idaClient.UpdateData();
                    }
                    catch (Exception e)
                    {
                        log.LogError(e, "IdaClient exception");
                    }
                }
                if (Configuration!.GetValue<bool>("Inara:Enabled"))
                {
                    try
                    {
                        await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                        if (ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, typeof(UpdateFromInara)) is UpdateFromInara updateFromInara)
                        {
                            await updateFromInara.Update();
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError(e, "Inara update exception");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
            }
        }

        private static async Task CommanderJournalUpdateQueue(CancellationToken cancellationToken)
        {
            Endpoint activeMqEndpont = Endpoint.Create(
               Configuration!.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
               Configuration!.GetValue<int?>("ActiveMQ:Port") ?? throw new Exception("No ActiveMQ port configured"),
               Configuration!.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
               Configuration!.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
            await using IProducer commanderCApiproducer = await connection.CreateProducerAsync("Commander.CApi", RoutingType.Anycast, cancellationToken);

            ILogger log = Services!.GetRequiredService<ILogger<Program>>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using AsyncServiceScope serviceScope = Services!.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    List<Commander> commanders =  await dbContext.Commanders
                        .AsNoTracking()
                        .Where(c => c.OAuthStatus == CommanderOAuthStatus.Active && c.JournalLastProcessed < DateTimeOffset.Now.AddMinutes(-30))
                        .ToListAsync(cancellationToken);
                    foreach (Commander commander in commanders)
                    {
                        await commanderCApiproducer.SendAsync(new Message(JsonConvert.SerializeObject(new CommanderCApi(commander.FDevCustomerId))), cancellationToken);
                    }
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                }
                catch (Exception e)
                {
                    log.LogError(e, "Commander journal selection update exception");
                }
            }
        }
    }
}