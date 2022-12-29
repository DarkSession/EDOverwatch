using EDUtils;
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
                _ = StationUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = StarSystemFssSignalsUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = ThargoidMaelstromCreatedUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = StarSystemThargoidManualUpdateConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);

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

        private async Task StationUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            try
            {
                await using IConsumer consumer = await connection.CreateConsumerAsync(StationUpdated.QueueName, StationUpdated.Routing, cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Message message = await consumer.ReceiveAsync(cancellationToken);
                        await using Transaction transaction = new();
                        await consumer.AcceptAsync(message, transaction, cancellationToken);

                        string jsonString = message.GetBody<string>();
                        StationUpdated? stationUpdated = JsonConvert.DeserializeObject<StationUpdated>(jsonString);
                        if (stationUpdated != null)
                        {
                            using AsyncLockInstance l = await Lock(cancellationToken);
                            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

                            Station? station = await dbContext.Stations.FirstOrDefaultAsync(s => s.MarketId == stationUpdated.MarketId, cancellationToken);
                            if (station?.State == StationState.UnderRepairs)
                            {
                                StarSystem? starSystem = await dbContext.StarSystems
                                    .Include(s => s.ThargoidLevel)
                                    .ThenInclude(t => t!.Maelstrom)
                                    .Include(s => s.ThargoidLevel!.CycleEnd)
                                    .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                                    .FirstOrDefaultAsync(s => s.SystemAddress == stationUpdated.SystemAddress, cancellationToken);
                                if (starSystem?.ThargoidLevel != null &&
                                    starSystem.ThargoidLevel.Maelstrom is ThargoidMaelstrom maelstrom &&
                                    starSystem.ThargoidLevel.State != StarSystemThargoidLevelState.None &&
                                    starSystem.ThargoidLevel.State != StarSystemThargoidLevelState.Recovery)
                                {
                                    await UpdateStarSystemThargoidLevel(starSystem, false, null, StarSystemThargoidLevelState.Recovery, maelstrom, dbContext, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                                }
                            }
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Exception while processing station update event");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "StationUpdatedEventConsumer exception");
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

        private async Task ThargoidMaelstromCreatedUpdatedEventConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            try
            {
                await using IConsumer consumer = await connection.CreateConsumerAsync(ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing, cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Message message = await consumer.ReceiveAsync(cancellationToken);
                        await using Transaction transaction = new();
                        await consumer.AcceptAsync(message, transaction, cancellationToken);

                        string jsonString = message.GetBody<string>();
                        ThargoidMaelstromCreatedUpdated? thargoidMaelstromCreatedUpdated = JsonConvert.DeserializeObject<ThargoidMaelstromCreatedUpdated>(jsonString);
                        if (thargoidMaelstromCreatedUpdated != null)
                        {
                            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

                            ThargoidMaelstrom? maelstrom = await dbContext.ThargoidMaelstroms
                                .Include(t => t.StarSystem)
                                .FirstOrDefaultAsync(t => t.Id == thargoidMaelstromCreatedUpdated.Id, cancellationToken);
                            if (maelstrom?.StarSystem != null)
                            {
                                using AsyncLockInstance l = await Lock(cancellationToken);
                                await CheckStarSystem(maelstrom.StarSystem.SystemAddress, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                            }
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Exception while processing maelstrom created/updated event");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "ThargoidMaelstromCreatedUpdatedEventConsumer exception");
            }
        }

        private async Task StarSystemThargoidManualUpdateConsumer(IConnection connection, IProducer starSystemThargoidLevelChangedProducer, CancellationToken cancellationToken)
        {
            try
            {
                await using IAnonymousProducer anonymousProducer = await connection.CreateAnonymousProducerAsync(cancellationToken);
                await using IConsumer consumer = await connection.CreateConsumerAsync(StarSystemThargoidManualUpdate.QueueName, StarSystemThargoidManualUpdate.Routing, cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Message message = await consumer.ReceiveAsync(cancellationToken);
                        await using Transaction transaction = new();
                        await consumer.AcceptAsync(message, transaction, cancellationToken);

                        string jsonString = message.GetBody<string>();
                        StarSystemThargoidManualUpdate? starSystemThargoidManualUpdate = JsonConvert.DeserializeObject<StarSystemThargoidManualUpdate>(jsonString);
                        if (starSystemThargoidManualUpdate != null)
                        {
                            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

                            StarSystem? starSystem = null;
                            if (starSystemThargoidManualUpdate.SystemAddress > 0)
                            {
                                starSystem = await dbContext.StarSystems
                                    .Include(s => s.ThargoidLevel)
                                    .ThenInclude(t => t!.Maelstrom)
                                    .Include(s => s.ThargoidLevel!.CycleEnd)
                                    .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                                    .FirstOrDefaultAsync(s => s.SystemAddress == starSystemThargoidManualUpdate.SystemAddress, cancellationToken);
                            }
                            else if (!string.IsNullOrEmpty(starSystemThargoidManualUpdate.SystemName))
                            {
                                starSystem = await dbContext.StarSystems
                                        .Include(s => s.ThargoidLevel)
                                        .ThenInclude(t => t!.Maelstrom)
                                        .Include(s => s.ThargoidLevel!.CycleEnd)
                                        .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                                        .FirstOrDefaultAsync(s =>
                                            EF.Functions.Like(s.Name, starSystemThargoidManualUpdate.SystemName.Replace("%", string.Empty)) &&
                                            s.ThargoidLevel != null &&
                                            s.ThargoidLevel.State > StarSystemThargoidLevelState.None, cancellationToken);
                                if (starSystem == null)
                                {
                                    Log.LogWarning("Star system {name} not found!", starSystemThargoidManualUpdate.SystemName);
                                    List<string> allStarSystems = (await dbContext.StarSystems
                                        .AsNoTracking()
                                        .Where(s =>
                                            s.ThargoidLevel != null &&
                                            s.ThargoidLevel.State > StarSystemThargoidLevelState.None)
                                        .Select(s => s.Name)
                                        .ToListAsync(cancellationToken)).Select(s => s.ToUpper()).ToList();

                                    List<string> similarStarSystemNames = allStarSystems
                                        .Where(a => StringUtil.ComputeSimilarity(a, starSystemThargoidManualUpdate.SystemName) <= 1)
                                        .ToList();
                                    if (similarStarSystemNames.Any())
                                    {
                                        Log.LogWarning("Found {count} similar star system names", similarStarSystemNames.Count);
                                        if (similarStarSystemNames.Count == 1)
                                        {
                                            string starSystemName = similarStarSystemNames.First();
                                            starSystem = await dbContext.StarSystems
                                                                .Include(s => s.ThargoidLevel)
                                                                .ThenInclude(t => t!.Maelstrom)
                                                                .Include(s => s.ThargoidLevel!.CycleEnd)
                                                                .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                                                                .FirstOrDefaultAsync(s =>
                                                                    EF.Functions.Like(s.Name, starSystemName) &&
                                                                    s.ThargoidLevel != null &&
                                                                    s.ThargoidLevel.State > StarSystemThargoidLevelState.None, cancellationToken);
                                        }
                                    }
                                }
                            }

                            if (starSystem != null)
                            {
                                bool changed = false;
                                using AsyncLockInstance l = await Lock(cancellationToken);
                                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken);
                                TimeSpan timeSinceLastTick = DateTimeOffset.UtcNow - currentThargoidCycle.Start;
                                TimeSpan signalSourceMaxAge = (timeSinceLastTick > TimeSpan.FromDays(1)) ? timeSinceLastTick : TimeSpan.FromDays(1);
                                (_, ThargoidMaelstrom? maelstrom) = await AnalyzeThargoidLevelForSystem(starSystem, signalSourceMaxAge, dbContext, cancellationToken);
                                if (maelstrom != null)
                                {
                                    changed = await UpdateStarSystemThargoidLevel(starSystem, true, starSystemThargoidManualUpdate.Progress, starSystemThargoidManualUpdate.State, maelstrom, dbContext, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                                }

                                List<StarSystemUpdateQueueItem> starSystemUpdateQueueItems = await dbContext.StarSystemUpdateQueueItems
                                    .Where(s => s.StarSystem == starSystem && s.Status == StarSystemUpdateQueueItemStatus.PendingAutomaticReview)
                                    .ToListAsync(cancellationToken);
                                foreach (StarSystemUpdateQueueItem starSystemUpdateQueueItem in starSystemUpdateQueueItems)
                                {
                                    starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.PendingNotification;
                                    starSystemUpdateQueueItem.Completed = DateTimeOffset.Now;
                                    starSystemUpdateQueueItem.Result = changed ? StarSystemUpdateQueueItemResult.Updated : StarSystemUpdateQueueItemResult.NotUpdated;
                                    starSystemUpdateQueueItem.ResultBy = StarSystemUpdateQueueItemResultBy.Automatic;

                                    StarSystemUpdateQueueItemUpdated starSystemUpdateQueueItemUpdated = new(starSystemUpdateQueueItem.Id);
                                    await anonymousProducer.SendAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing, starSystemUpdateQueueItemUpdated.Message, transaction, cancellationToken);
                                }

                                await dbContext.SaveChangesAsync(cancellationToken);
                            }
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Exception while processing maelstrom created/updated event");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "StarSystemThargoidManualUpdateConsumer exception");
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
                .Include(s => s.ThargoidLevel!.ManualUpdateCycle)
                .SingleOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem != null)
            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken);
                TimeSpan timeSinceLastTick = DateTimeOffset.UtcNow - currentThargoidCycle.Start;
                TimeSpan signalSourceMaxAge = (timeSinceLastTick > TimeSpan.FromDays(1)) ? timeSinceLastTick : TimeSpan.FromDays(1);

                (StarSystemThargoidLevelState newThargoidLevel, ThargoidMaelstrom? maelstrom) = await AnalyzeThargoidLevelForSystem(starSystem, signalSourceMaxAge, dbContext, cancellationToken);
                // If the system is brand new, we might not have all the data yet, so we skip it for now.
                if (newThargoidLevel == StarSystemThargoidLevelState.None && starSystem.Created > DateTimeOffset.UtcNow.AddHours(-6))
                {
                    return;
                }
                if (maelstrom != null)
                {
                    await UpdateStarSystemThargoidLevel(starSystem, false, null, newThargoidLevel, maelstrom, dbContext, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                }
                if (!starSystem.WarRelevantSystem && (starSystem.RefreshedWarRelevantSystem || newThargoidLevel != StarSystemThargoidLevelState.None))
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

        private async Task<bool> UpdateStarSystemThargoidLevel(StarSystem starSystem, bool isManualUpdate, short? progress, StarSystemThargoidLevelState newThargoidLevel, ThargoidMaelstrom maelstrom, EdDbContext dbContext, IProducer starSystemThargoidLevelChangedProducer, Transaction transaction, CancellationToken cancellationToken)
        {
            if (isManualUpdate ||
                starSystem.ThargoidLevel?.State == null ||
                starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Alert ||
                (starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Controlled && newThargoidLevel == StarSystemThargoidLevelState.None) ||
                starSystem.ThargoidLevel.State < newThargoidLevel)
            {
                if (starSystem.ThargoidLevel?.State == newThargoidLevel && 
                    (progress == null || starSystem.ThargoidLevel?.Progress == progress))
                {
                    return false;
                }
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken);
                if (!isManualUpdate && starSystem.ThargoidLevel?.ManualUpdateCycle?.Id == currentThargoidCycle.Id)
                {
                    return false;
                }
                if (starSystem.ThargoidLevel != null)
                {
                    if (starSystem.ThargoidLevel.CycleStartId == currentThargoidCycle.Id)
                    {
                        Log.LogWarning("Star System {systemAddress} ({systemName}): New thargoid level in the same cycle! {currentLevel} -> {newLevel}", starSystem.SystemAddress, starSystem.Name, starSystem.ThargoidLevel.State, newThargoidLevel);
                    }
                    starSystem.ThargoidLevel.CycleEnd = await dbContext.GetThargoidCycle(starSystem.Updated, cancellationToken, -1);
                }
                starSystem.ThargoidLevel = new(0, newThargoidLevel, null, DateTimeOffset.UtcNow)
                {
                    StarSystem = starSystem,
                    CycleStart = currentThargoidCycle,
                    Maelstrom = maelstrom,
                    Progress = progress,
                };
                if (newThargoidLevel == StarSystemThargoidLevelState.Alert)
                {
                    starSystem.ThargoidLevel.StateExpires = currentThargoidCycle;
                }
                if (isManualUpdate)
                {
                    starSystem.ThargoidLevel.ManualUpdateCycle = currentThargoidCycle;
                }
                await dbContext.SaveChangesAsync(cancellationToken);

                StarSystemThargoidLevelChanged starSystemThargoidLevelChanged = new(starSystem.SystemAddress);
                await starSystemThargoidLevelChangedProducer.SendAsync(starSystemThargoidLevelChanged.Message, transaction, cancellationToken);
                return true;
            }
            else if (starSystem.ThargoidLevel != null && starSystem.ThargoidLevel.Maelstrom == null)
            {
                starSystem.ThargoidLevel.Maelstrom = maelstrom;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            if (maelstrom.StarSystem != null && newThargoidLevel != StarSystemThargoidLevelState.None)
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
            return false;
        }
    }
}
