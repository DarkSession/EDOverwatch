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
        private decimal MaelstromMaxDistanceLy { get; set; } = 40m;

        public Overwatch(IConfiguration configuration, ILogger<Overwatch> log, IServiceProvider serviceProvider)
        {
            Configuration = configuration;
            Log = log;
            ServiceProvider = serviceProvider;
        }

        private Task<AsyncLockInstance> Lock(CancellationToken cancellationToken) => AsyncLock.AquireLockInstance(SemaphoreSlimLock, cancellationToken);

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Log.LogInformation("Starting Overwatch");

                Endpoint activeMqEndpont = Endpoint.Create(
                    Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                    Configuration.GetValue<int>("ActiveMQ:Port"),
                    Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                    Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

                {
                    await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    if (await dbContext.ThargoidMaelstroms.AnyAsync(cancellationToken))
                    {
                        MaelstromMaxDistanceLy = (await dbContext.ThargoidMaelstroms.MaxAsync(t => t.InfluenceSphere, cancellationToken)) + 10m;
                        Log.LogInformation("MaelstromMaxDistanceLy: {MaelstromMaxDistanceLy}", MaelstromMaxDistanceLy);
                    }
                }

                ConnectionFactory connectionFactory = new();
                await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont, cancellationToken);
                await using IProducer starSystemThargoidLevelChangedProducer = await connection.CreateProducerAsync(StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing, cancellationToken);

                _ = StarSystemUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = StarSystemFssSignalsUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);

                Log.LogInformation("Overwatch started");

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Overwatch exception");
            }
        }

        private async Task StarSystemUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            try
            {
                await using IConsumer consumer = await connection.CreateConsumerAsync(StarSystemUpdated.QueueName, StarSystemUpdated.Routing, cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Message message = await consumer.ReceiveAsync(cancellationToken);

                        Log.LogDebug("{n} received", StarSystemUpdated.QueueName);

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
            catch (Exception e)
            {
                Log.LogError(e, "StarSystemUpdatedEventConsumer exception");
            }
        }

        private async Task StarSystemFssSignalsUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            try
            {
                await using IConsumer consumer = await connection.CreateConsumerAsync(StarSystemFssSignalsUpdated.QueueName, StarSystemFssSignalsUpdated.Routing, cancellationToken);
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
            catch (Exception e)
            {
                Log.LogError(e, "StarSystemFssSignalsUpdatedEventConsumer exception");
            }
        }

        private async Task CheckStarSystem(long systemAddress, IProducer starSystemThargoidLevelChangedProducer, Transaction transaction, CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

            StarSystem? starSystem = await dbContext.StarSystems
                .Include(s => s.Allegiance)
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .Include(s => s.ThargoidLevel!.CycleStart)
                .SingleOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem != null)
            {
                (StarSystemThargoidLevelState thargoidLevel, ThargoidMaelstrom? maelstrom) = await AnalyzeThargoidLevelForSystem(starSystem, TimeSpan.FromDays(1), dbContext, cancellationToken);
                // If the system is brand new, we might not have all the data yet, so we skip it for now.
                if (thargoidLevel == StarSystemThargoidLevelState.None && starSystem.Created > DateTimeOffset.UtcNow.AddHours(-6))
                {
                    return;
                }
                if (maelstrom != null)
                {
                    // If the level decreased in the same cycle, we might have some old data, so we analyze again and allow to use older signal sources
                    if ((starSystem.ThargoidLevel?.CycleStart?.IsCurrent ?? false) && starSystem.ThargoidLevel.State > thargoidLevel)
                    {
                        (thargoidLevel, _) = await AnalyzeThargoidLevelForSystem(starSystem, TimeSpan.FromDays(4), dbContext, cancellationToken);
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
                        starSystem.ThargoidLevel = new(0, thargoidLevel, null, DateTimeOffset.UtcNow)
                        {
                            StarSystem = starSystem,
                            CycleStart = currentThargoidCycle,
                            Maelstrom = maelstrom,
                        };
                        await dbContext.SaveChangesAsync(cancellationToken);

                        StarSystemThargoidLevelChanged starSystemThargoidLevelChanged = new(starSystem.SystemAddress);
                        await starSystemThargoidLevelChangedProducer.SendAsync(starSystemThargoidLevelChanged.Message, transaction, cancellationToken);
                    }
                    else if (starSystem.ThargoidLevel != null && starSystem.ThargoidLevel.Maelstrom == null)
                    {
                        starSystem.ThargoidLevel.Maelstrom = maelstrom;
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                    if (maelstrom.StarSystem != null && thargoidLevel != StarSystemThargoidLevelState.None)
                    {
                        decimal distanceToMaelstrom = (decimal)starSystem.DistanceTo(maelstrom.StarSystem);
                        if (maelstrom.InfluenceSphere < distanceToMaelstrom)
                        {
                            maelstrom.InfluenceSphere = distanceToMaelstrom;
                            Log.LogInformation("Maelstrom {name}'s sphere of influence is to {distanceToMaelstrom}", maelstrom.Name, distanceToMaelstrom);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                        distanceToMaelstrom += 10m;
                        if (distanceToMaelstrom > MaelstromMaxDistanceLy)
                        {
                            MaelstromMaxDistanceLy = distanceToMaelstrom;
                            Log.LogInformation("MaelstromMaxDistanceLy: {MaelstromMaxDistanceLy}", MaelstromMaxDistanceLy);
                        }
                    }
                }
                if (!starSystem.WarRelevantSystem && (starSystem.IsWarRelevantSystem || thargoidLevel != StarSystemThargoidLevelState.None))
                {
                    starSystem.WarRelevantSystem = true;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task<(StarSystemThargoidLevelState level, ThargoidMaelstrom? maelstrom)> AnalyzeThargoidLevelForSystem(StarSystem starSystem, TimeSpan maxAge, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            StarSystemThargoidLevelState thargoidLevel = StarSystemThargoidLevelState.None;
            // We check if the system is within range of a Maelstrom
            ThargoidMaelstrom? maelstrom = await dbContext.ThargoidMaelstroms
                .Include(t => t.StarSystem)
                .FirstOrDefaultAsync(t =>
                        t.StarSystem!.LocationX >= starSystem.LocationX - MaelstromMaxDistanceLy && t.StarSystem!.LocationX <= starSystem.LocationX + MaelstromMaxDistanceLy &&
                        t.StarSystem!.LocationY >= starSystem.LocationY - MaelstromMaxDistanceLy && t.StarSystem!.LocationY <= starSystem.LocationY + MaelstromMaxDistanceLy &&
                        t.StarSystem!.LocationZ >= starSystem.LocationZ - MaelstromMaxDistanceLy && t.StarSystem!.LocationZ <= starSystem.LocationZ + MaelstromMaxDistanceLy, cancellationToken);
            if (maelstrom != null)
            {
                DateTimeOffset signalsMaxAge = starSystem.Updated.Subtract(maxAge);
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
            }
            return (thargoidLevel, maelstrom);
        }
    }
}
