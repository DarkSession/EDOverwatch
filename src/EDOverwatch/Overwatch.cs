using Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EDOverwatch
{
    internal class Overwatch
    {
        private IConfiguration Configuration { get; }
        private SemaphoreSlim SemaphoreSlimLock { get; } = new SemaphoreSlim(1, 1);
        private ILogger Log { get; }
        private IServiceProvider ServiceProvider { get; }

        public Overwatch(IConfiguration configuration, ILogger<Overwatch> log, IServiceProvider serviceProvider)
        {
            Configuration = configuration;
            Log = log;
            ServiceProvider = serviceProvider;
        }

        private Task<AsyncLockInstance> Lock(CancellationToken cancellationToken) => AsyncLock.AquireLockInstance(SemaphoreSlimLock, cancellationToken);

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Endpoint activeMqEndpont = Endpoint.Create(
                Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                Configuration.GetValue<int>("ActiveMQ:Port"),
                Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
            await using IProducer starSystemThargoidLevelChangedProducer = await connection.CreateProducerAsync("StarSystem.ThargoidLevelChanged", RoutingType.Anycast, cancellationToken);

            _ = StarSystemUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
            _ = StarSystemFssSignalsUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        private async Task StarSystemUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            await using IConsumer consumer = await connection.CreateConsumerAsync("StarSystem.Updated", RoutingType.Anycast, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync(cancellationToken);
                    await using Transaction transaction = new();
                    await consumer.AcceptAsync(message, transaction, cancellationToken);

                    string jsonString = message.GetBody<string>();
                    StarSystemUpdated? starSystemUpdated = JsonConvert.DeserializeObject<StarSystemUpdated>(jsonString);
                    if (starSystemUpdated != null)
                    {
                        using AsyncLockInstance l = await Lock(cancellationToken);
                        await CheckStarSystem(starSystemUpdated.SystemAddress, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Exception while processing star system updated event");
                }
            }
        }

        private async Task StarSystemFssSignalsUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            await using IConsumer consumer = await connection.CreateConsumerAsync("StarSystem.FssSignalsUpdated", RoutingType.Anycast, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync(cancellationToken);
                    await using Transaction transaction = new();
                    await consumer.AcceptAsync(message, transaction, cancellationToken);

                    string jsonString = message.GetBody<string>();
                    StarSystemFssSignalsUpdated? starSystemFssSignalsUpdated = JsonConvert.DeserializeObject<StarSystemFssSignalsUpdated>(jsonString);
                    if (starSystemFssSignalsUpdated != null)
                    {
                        using AsyncLockInstance l = await Lock(cancellationToken);
                        await CheckStarSystem(starSystemFssSignalsUpdated.SystemAddress, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Exception while processing fss signals updated event");
                }
            }
        }

        private const decimal MaelstromMaxDistanceLy = 40m;

        private async Task CheckStarSystem(long systemAddress, IProducer starSystemThargoidLevelChangedProducer, Transaction transaction, CancellationToken cancellationToken)
        {
            using IServiceScope serviceScope = ServiceProvider.CreateScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

            StarSystem? starSystem = await dbContext.StarSystems
                .Include(s => s.Allegiance)
                .Include(s => s.ThargoidLevel)
                .SingleOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem != null &&
                // We check if the system is within range of a Maelstrom
                await dbContext.ThargoidMaelstroms.AnyAsync(t =>
                            t.StarSystem!.LocationX >= starSystem.LocationX - MaelstromMaxDistanceLy && t.StarSystem!.LocationX <= starSystem.LocationX + MaelstromMaxDistanceLy &&
                            t.StarSystem!.LocationY >= starSystem.LocationY - MaelstromMaxDistanceLy && t.StarSystem!.LocationY <= starSystem.LocationY + MaelstromMaxDistanceLy &&
                            t.StarSystem!.LocationZ >= starSystem.LocationZ - MaelstromMaxDistanceLy && t.StarSystem!.LocationZ <= starSystem.LocationZ + MaelstromMaxDistanceLy, cancellationToken))
            {
                StarSystemThargoidLevelState thargoidLevel = StarSystemThargoidLevelState.Unknown;
                DateTimeOffset signalsMaxAge = starSystem.Updated.AddHours(-6);
                IQueryable<StarSystemFssSignal> signalQuery = dbContext.StarSystemFssSignals.Where(s => s.StarSystem == starSystem && s.LastSeen > signalsMaxAge);
                if (starSystem.Allegiance?.IsThargoid ?? false)
                {
                    if (await signalQuery.AnyAsync(s => s.Type == StarSystemFssSignalType.Maelstrom, cancellationToken))
                    {
                        thargoidLevel = StarSystemThargoidLevelState.Maelstrom;
                    }
                    else
                    {
                        thargoidLevel = StarSystemThargoidLevelState.Controlled;
                    }
                }
                else if (await signalQuery.AnyAsync(s => s.Type == StarSystemFssSignalType.AXCZ, cancellationToken))
                {
                    thargoidLevel = StarSystemThargoidLevelState.Invasion;
                }
                else if (await signalQuery.AnyAsync(s => s.Type == StarSystemFssSignalType.ThargoidActivity, cancellationToken))
                {
                    thargoidLevel = StarSystemThargoidLevelState.Alert;
                }

                // If the system is brand new, we might not have all the data yet, so we skip it for now.
                if (thargoidLevel == StarSystemThargoidLevelState.Unknown && starSystem.Created > DateTimeOffset.UtcNow.AddHours(-6))
                {
                    return;
                }

                if (starSystem.ThargoidLevel?.State != thargoidLevel)
                {
                    ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken);
                    if (starSystem.ThargoidLevel != null)
                    {
                        if (starSystem.ThargoidLevel.CycleStart?.Id == currentThargoidCycle.Id)
                        {
                            Log.LogWarning("Star System {systemAddress}: New thargoid level in the same cycle!", starSystem.SystemAddress);
                        }
                        starSystem.ThargoidLevel.CycleEnd = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken, -1);
                    }
                    starSystem.ThargoidLevel = new(0, thargoidLevel)
                    {
                        StarSystem = starSystem,
                        CycleStart = currentThargoidCycle,
                    };
                    await dbContext.SaveChangesAsync(cancellationToken);
                    // await starSystemThargoidLevelChangedProducer.SendAsync(new(JsonConvert.SerializeObject(new StarSystemThargoidLevelChanged(starSystem.SystemAddress))), transaction, cancellationToken);
                }
            }
        }
    }
}
