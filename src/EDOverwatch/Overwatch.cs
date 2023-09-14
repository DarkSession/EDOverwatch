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

                _ = ThargoidMaelstromCreatedUpdatedEventConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = StarSystemThargoidManualUpdateConsumer(connection, starSystemThargoidLevelChangedProducer, cancellationToken);
                _ = QueueUpdates(cancellationToken);

                Log.LogInformation("Overwatch started");

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Overwatch exception");
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
                                .ThenInclude(s => s!.ThargoidLevel)
                                .FirstOrDefaultAsync(t => t.Id == thargoidMaelstromCreatedUpdated.Id, cancellationToken);
                            if (maelstrom?.StarSystem != null)
                            {
                                using AsyncLockInstance l = await Lock(cancellationToken);
                                if (maelstrom.StarSystem.ThargoidLevel?.State != StarSystemThargoidLevelState.Titan)
                                {
                                    await UpdateStarSystemThargoidLevel(maelstrom.StarSystem, false, null, TimeSpan.Zero, StarSystemThargoidLevelState.Titan, maelstrom, dbContext, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                                    await dbContext.SaveChangesAsync(cancellationToken);
                                }
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
                                    .Include(s => s.ThargoidLevel!.CurrentProgress)
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
                                if (await GetMaelstromForSystem(starSystem, dbContext, cancellationToken) is ThargoidMaelstrom maelstrom)
                                {
                                    TimeSpan timeLeft = TimeSpan.Zero;
                                    if (starSystemThargoidManualUpdate.DaysLeft is short daysLeft && daysLeft > 0)
                                    {
                                        timeLeft = TimeSpan.FromDays(daysLeft);
                                    }
                                    changed = await UpdateStarSystemThargoidLevel(starSystem, true, starSystemThargoidManualUpdate.Progress, timeLeft, starSystemThargoidManualUpdate.State, maelstrom, dbContext, starSystemThargoidLevelChangedProducer, transaction, cancellationToken);
                                }

                                if (!starSystem.WarRelevantSystem && (starSystem.RefreshedWarRelevantSystem || starSystemThargoidManualUpdate.State != StarSystemThargoidLevelState.None))
                                {
                                    starSystem.WarRelevantSystem = true;
                                    await dbContext.SaveChangesAsync(cancellationToken);
                                }

                                List<StarSystemUpdateQueueItem> starSystemUpdateQueueItems = await dbContext.StarSystemUpdateQueueItems
                                    .Where(s => s.StarSystem == starSystem && s.Status == StarSystemUpdateQueueItemStatus.PendingAutomaticReview)
                                    .ToListAsync(cancellationToken);
                                foreach (StarSystemUpdateQueueItem starSystemUpdateQueueItem in starSystemUpdateQueueItems)
                                {
                                    starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.PendingNotification;
                                    starSystemUpdateQueueItem.Completed = DateTimeOffset.UtcNow;
                                    starSystemUpdateQueueItem.Result = changed ? StarSystemUpdateQueueItemResult.Updated : StarSystemUpdateQueueItemResult.NotUpdated;
                                    starSystemUpdateQueueItem.ResultBy = StarSystemUpdateQueueItemResultBy.Automatic;

                                    StarSystemUpdateQueueItemUpdated starSystemUpdateQueueItemUpdated = new(starSystemUpdateQueueItem.Id);
                                    await anonymousProducer.SendAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing, starSystemUpdateQueueItemUpdated.Message, transaction, cancellationToken);
                                }

                                await dbContext.SaveChangesAsync(cancellationToken);
                            }
                        }

                        await consumer.AcceptAsync(message, transaction, cancellationToken);
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

        private async Task<ThargoidMaelstrom?> GetMaelstromForSystem(StarSystem starSystem, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            List<ThargoidMaelstrom> maelstroms = await dbContext.ThargoidMaelstroms
                .Include(t => t.StarSystem)
                .Where(t =>
                        t.StarSystem!.LocationX >= starSystem.LocationX - MaelstromMaxDistanceLy && t.StarSystem!.LocationX <= starSystem.LocationX + MaelstromMaxDistanceLy &&
                        t.StarSystem!.LocationY >= starSystem.LocationY - MaelstromMaxDistanceLy && t.StarSystem!.LocationY <= starSystem.LocationY + MaelstromMaxDistanceLy &&
                        t.StarSystem!.LocationZ >= starSystem.LocationZ - MaelstromMaxDistanceLy && t.StarSystem!.LocationZ <= starSystem.LocationZ + MaelstromMaxDistanceLy)
                .ToListAsync(cancellationToken);
            return maelstroms
                .OrderBy(m => m.StarSystem?.DistanceTo(starSystem) ?? 999)
                .FirstOrDefault();
        }

        private async Task<bool> UpdateStarSystemThargoidLevel(
            StarSystem starSystem,
            bool isManualUpdate,
            short? progress,
            TimeSpan remainingTime,
            StarSystemThargoidLevelState newThargoidLevel,
            ThargoidMaelstrom maelstrom,
            EdDbContext dbContext,
            IProducer starSystemThargoidLevelChangedProducer,
            Transaction transaction,
            CancellationToken cancellationToken)
        {
            if (isManualUpdate ||
                starSystem.ThargoidLevel?.State == null ||
                (starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Controlled && newThargoidLevel == StarSystemThargoidLevelState.None) ||
                starSystem.ThargoidLevel.State < newThargoidLevel)
            {
                if (starSystem.ThargoidLevel?.State == newThargoidLevel &&
                    (progress == null || starSystem.ThargoidLevel?.Progress == progress))
                {
                    return false;
                }
                DateTimeOffset time = isManualUpdate ? DateTimeOffset.UtcNow : starSystem.Updated;
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(time, cancellationToken);
                if (!isManualUpdate && starSystem.ThargoidLevel?.ManualUpdateCycle?.Id == currentThargoidCycle.Id)
                {
                    return false;
                }
                StarSystemThargoidLevel? thargoidLevel = starSystem.ThargoidLevel;
                ThargoidCycle? stateExpires = null;
                if (remainingTime > TimeSpan.Zero)
                {
                    DateTimeOffset remainingTimeEnd = DateTimeOffset.UtcNow.Add(remainingTime);
                    if (remainingTimeEnd.DayOfWeek == DayOfWeek.Wednesday || (remainingTimeEnd.DayOfWeek == DayOfWeek.Thursday && remainingTimeEnd.Hour < 7))
                    {
                        remainingTimeEnd = new DateTimeOffset(remainingTimeEnd.Year, remainingTimeEnd.Month, remainingTimeEnd.Day, 0, 0, 0, TimeSpan.Zero);
                        stateExpires = await dbContext.GetThargoidCycle(remainingTimeEnd, cancellationToken);
                    }
                }

                if ((isManualUpdate && thargoidLevel?.State != newThargoidLevel) || thargoidLevel == null || thargoidLevel.State < newThargoidLevel)
                {
                    if (thargoidLevel != null)
                    {
                        if (thargoidLevel.CycleStartId == currentThargoidCycle.Id)
                        {
                            Log.LogWarning("Star System {systemAddress} ({systemName}): New thargoid level in the same cycle! {currentLevel} -> {newLevel}", starSystem.SystemAddress, starSystem.Name, thargoidLevel.State, newThargoidLevel);
                        }
                        thargoidLevel.CycleEnd = await dbContext.GetThargoidCycle(time, cancellationToken, -1);
                    }
                    thargoidLevel = new(0, newThargoidLevel, null, DateTimeOffset.UtcNow, false)
                    {
                        StarSystem = starSystem,
                        CycleStart = currentThargoidCycle,
                        Maelstrom = maelstrom,
                        Progress = progress,
                        StateExpires = stateExpires,
                    };
                    starSystem.ThargoidLevel = thargoidLevel;
                    if (thargoidLevel.State != StarSystemThargoidLevelState.None)
                    {
                        starSystem.WarAffected = true;
                    }
                    if (thargoidLevel.State == StarSystemThargoidLevelState.Alert)
                    {
                        List<Station> stations = await dbContext.Stations
                            .Include(s => s.MinorFaction)
                            .Include(s => s.PriorMinorFaction)
                            .Where(s => s.StarSystem == starSystem && s.PriorMinorFaction != s.MinorFaction)
                            .ToListAsync(cancellationToken);
                        foreach (Station station in stations)
                        {
                            station.PriorMinorFaction = station.MinorFaction;
                        }
                    }
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                if (progress != null && (isManualUpdate || (thargoidLevel.Progress ?? -1) <= progress))
                {
                    if (thargoidLevel.CurrentProgress == null || (thargoidLevel.Progress ?? -1) < progress)
                    {
                        StarSystemThargoidLevelProgress starSystemThargoidLevelProgress = new(0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, progress)
                        {
                            ThargoidLevel = starSystem.ThargoidLevel,
                        };
                        dbContext.StarSystemThargoidLevelProgress.Add(starSystemThargoidLevelProgress);
                        thargoidLevel.CurrentProgress = starSystemThargoidLevelProgress;
                    }
                    else
                    {
                        thargoidLevel.CurrentProgress.LastChecked = DateTimeOffset.UtcNow;
                    }
                    thargoidLevel.Progress = progress;
                }
                if (stateExpires != null)
                {
                    thargoidLevel.StateExpires = stateExpires;
                }
                if (newThargoidLevel == StarSystemThargoidLevelState.Alert)
                {
                    thargoidLevel.StateExpires = currentThargoidCycle;
                }
                if (isManualUpdate)
                {
                    thargoidLevel.ManualUpdateCycle = currentThargoidCycle;
                }
                if (!starSystem.WarRelevantSystem && (starSystem.RefreshedWarRelevantSystem || newThargoidLevel != StarSystemThargoidLevelState.None))
                {
                    starSystem.WarRelevantSystem = true;
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
                    Log.LogInformation("Maelstrom {name}'s sphere of influence extended to {distanceToMaelstrom}", maelstrom.Name, distanceToMaelstrom);
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

        private async Task QueueUpdates(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateMaelstromTotals(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "QueueUpdates exception");
                }
                try
                {
                    await UpdateAlertPredictions(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "QueueUpdates exception");
                }
                try
                {
                    await UpdateSystemWithReactivationMissions(cancellationToken);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "QueueUpdates exception");
                }
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        private async Task UpdateMaelstromTotals(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
            ThargoidCycle thargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);
            List<ThargoidMaelstromHistoricalSummary> thargoidMaelstromHistoricalCycleSummaries = await dbContext.ThargoidMaelstromHistoricalSummaries
                .Include(t => t.Maelstrom)
                .Where(t => t.Cycle == thargoidCycle)
                .ToListAsync(cancellationToken);
            var maelstromTotals = await dbContext.StarSystemThargoidLevels
                .Where(s => s.State > StarSystemThargoidLevelState.None &&
                    (s.CycleEnd == null || s.CycleStart!.Start <= s.CycleEnd.Start) &&
                    (
                        (s.CycleStart!.Start <= thargoidCycle.Start && s.CycleEnd == null) ||
                        (s.CycleStart!.Start <= thargoidCycle.Start && s.CycleEnd!.Start >= thargoidCycle.Start) ||
                        (s.CycleStart!.Start >= thargoidCycle.Start && s.CycleEnd!.Start <= thargoidCycle.Start)
                    ))
                .GroupBy(s => new { s.MaelstromId, s.State })
                .Select(s => new
                {
                    s.Key.MaelstromId,
                    s.Key.State,
                    Count = s.Count(),
                })
                .ToListAsync(cancellationToken);
            foreach (var maelstromTotal in maelstromTotals)
            {
                ThargoidMaelstrom maelstrom = await dbContext.ThargoidMaelstroms.FirstAsync(t => t.Id == maelstromTotal.MaelstromId, cancellationToken);
                ThargoidMaelstromHistoricalSummary? thargoidMaelstromHistoricalSummary = thargoidMaelstromHistoricalCycleSummaries.FirstOrDefault(t => t.Maelstrom?.Id == maelstrom.Id && t.State == maelstromTotal.State);
                if (thargoidMaelstromHistoricalSummary == null)
                {
                    thargoidMaelstromHistoricalSummary = new(0, maelstromTotal.State, maelstromTotal.Count)
                    {
                        Maelstrom = maelstrom,
                        Cycle = thargoidCycle,
                    };
                    dbContext.ThargoidMaelstromHistoricalSummaries.Add(thargoidMaelstromHistoricalSummary);
                }
                else
                {
                    thargoidMaelstromHistoricalSummary.Amount = maelstromTotal.Count;
                    thargoidMaelstromHistoricalCycleSummaries.Remove(thargoidMaelstromHistoricalSummary);
                }
            }

            if (thargoidMaelstromHistoricalCycleSummaries.Any())
            {
                dbContext.ThargoidMaelstromHistoricalSummaries.RemoveRange(thargoidMaelstromHistoricalCycleSummaries);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateAlertPredictions(CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now.DayOfWeek == DayOfWeek.Thursday && now.Hour >= 7 && now.Hour <= 8)
            {
                return;
            }

            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

            {
                ThargoidCycle currentThargoidCycle = await dbContext.GetThargoidCycle(cancellationToken);
                if (!await dbContext.AlertPredictionCycleAttackers.AnyAsync(a => a.Cycle == currentThargoidCycle, cancellationToken))
                {
                    await EDOverwatchAlertPrediction.AlertPrediction.UpdateAttackersForCycle(dbContext, currentThargoidCycle, cancellationToken);
                }
            }

            ThargoidCycle nextThargoidCycle = await dbContext.GetThargoidCycle(now, cancellationToken, 1);
            await EDOverwatchAlertPrediction.AlertPrediction.PredictionForCycle(dbContext, nextThargoidCycle, cancellationToken);
        }

        private async Task UpdateSystemWithReactivationMissions(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
            EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();

            List<StarSystem> thargoidControlledWithMilitarySettlements = await dbContext.StarSystems
                .AsNoTracking()
                .Where(s => s.ThargoidLevel!.State == StarSystemThargoidLevelState.Controlled && s.Stations!.Any(s => s.Type!.Name == StationType.OdysseySettlementType && s.PrimaryEconomy!.Name == Economy.Military))
                .ToListAsync(cancellationToken);

            List<long> applicableSystems = new();
            decimal maxDistance = 20m;

            foreach (StarSystem thargoidControlledWithMilitarySettlement in thargoidControlledWithMilitarySettlements)
            {
                List<StarSystem> nearbyStarSystems = await dbContext.StarSystems
                    .Where(s => s.WarAffected && s.ThargoidLevel != null && s.ThargoidLevel.State != StarSystemThargoidLevelState.None && s.OriginalPopulation > 0)
                    .Where(s => s.LocationX >= thargoidControlledWithMilitarySettlement.LocationX - maxDistance && s.LocationX <= thargoidControlledWithMilitarySettlement.LocationX + maxDistance &&
                                s.LocationY >= thargoidControlledWithMilitarySettlement.LocationY - maxDistance && s.LocationY <= thargoidControlledWithMilitarySettlement.LocationY + maxDistance &&
                                s.LocationZ >= thargoidControlledWithMilitarySettlement.LocationZ - maxDistance && s.LocationZ <= thargoidControlledWithMilitarySettlement.LocationZ + maxDistance)
                    .ToListAsync(cancellationToken);
                foreach (StarSystem nearbyStarSystem in nearbyStarSystems)
                {
                    if (nearbyStarSystem.DistanceTo(thargoidControlledWithMilitarySettlement) > 20f)
                    {
                        continue;
                    }
                    nearbyStarSystem.ReactivationMissionsNearby = true;
                    applicableSystems.Add(nearbyStarSystem.SystemAddress);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            await dbContext.StarSystems
                .Where(s => s.ReactivationMissionsNearby && !applicableSystems.Contains(s.SystemAddress))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReactivationMissionsNearby, false), cancellationToken);
        }
    }
}
