using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "ApiKeyAuthentication")]
    public class UpdateController : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        public UpdateController(EdDbContext dbContext, ActiveMqMessageProducer producer)
        {
            DbContext = dbContext;
            Producer = producer;
        }

        [HttpPost]
        public async Task<ActionResult> Progress([FromBody] UpdateModel model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                StarSystem? starSystem = await DbContext.StarSystems
                    .Include(s => s.ThargoidLevel)
                    .FirstOrDefaultAsync(s => s.SystemAddress == model.SystemAddress, cancellationToken);
                if (starSystem?.ThargoidLevel == null)
                {
                    return NotFound();
                }
                if (starSystem.ThargoidLevel.State == model.SystemState && ((starSystem.ThargoidLevel.Progress ?? -1) < model.Progress))
                {
                    starSystem.ThargoidLevel.Progress = model.Progress;
                    StarSystemThargoidLevelProgress starSystemThargoidLevelProgress = new(0, DateTimeOffset.UtcNow, model.Progress)
                    {
                        ThargoidLevel = starSystem.ThargoidLevel,
                    };
                    DbContext.StarSystemThargoidLevelProgress.Add(starSystemThargoidLevelProgress);
                    starSystem.ThargoidLevel.CurrentProgress = starSystemThargoidLevelProgress;

                    TimeSpan remainingTime = TimeSpan.FromDays(model.DaysLeft ?? 0);
                    if (remainingTime > TimeSpan.Zero && starSystem.ThargoidLevel.StateExpires == null)
                    {
                        DateTimeOffset remainingTimeEnd = DateTimeOffset.UtcNow.Add(remainingTime);
                        if (remainingTimeEnd.DayOfWeek == DayOfWeek.Wednesday || (remainingTimeEnd.DayOfWeek == DayOfWeek.Thursday && remainingTimeEnd.Hour < 7))
                        {
                            remainingTimeEnd = new DateTimeOffset(remainingTimeEnd.Year, remainingTimeEnd.Month, remainingTimeEnd.Day, 0, 0, 0, TimeSpan.Zero);
                            ThargoidCycle thargoidCycle = await DbContext.GetThargoidCycle(remainingTimeEnd, CancellationToken.None);
                            starSystem.ThargoidLevel.StateExpires = thargoidCycle;
                        }
                    }
                    if (model.Progress >= 100)
                    {
                        await DbContext.DcohFactionOperations
                            .Where(d =>
                                d.StarSystem == starSystem &&
                                d.Status == DcohFactionOperationStatus.Active)
                            .ForEachAsync(d => d.Status = DcohFactionOperationStatus.Expired, cancellationToken);
                    }
                    await DbContext.SaveChangesAsync(cancellationToken);

                    StarSystemThargoidLevelChanged starSystemThargoidLevelChanged = new(starSystem.SystemAddress);
                    await Producer.SendAsync(StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing, starSystemThargoidLevelChanged.Message, cancellationToken);
                }
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<ActionResult> SystemUpdate([FromBody] UpdateModel model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                StarSystem? starSystem = await DbContext.StarSystems
                    .Include(s => s.ThargoidLevel)
                    .FirstOrDefaultAsync(s => s.SystemAddress == model.SystemAddress, cancellationToken);
                if (starSystem == null)
                {
                    return NotFound();
                }
                else if (starSystem.ThargoidLevel?.State == model.SystemState &&
                    (model.Progress == null || starSystem.ThargoidLevel?.Progress == model.Progress))
                {
                    List<StarSystemUpdateQueueItem> starSystemUpdateQueueItems = await DbContext.StarSystemUpdateQueueItems
                        .Where(s => s.StarSystem == starSystem && s.Status == StarSystemUpdateQueueItemStatus.PendingAutomaticReview)
                        .ToListAsync(cancellationToken);
                    foreach (StarSystemUpdateQueueItem starSystemUpdateQueueItem in starSystemUpdateQueueItems)
                    {
                        starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.PendingNotification;
                        starSystemUpdateQueueItem.Completed = DateTimeOffset.Now;
                        starSystemUpdateQueueItem.Result = StarSystemUpdateQueueItemResult.NotUpdated;
                        starSystemUpdateQueueItem.ResultBy = StarSystemUpdateQueueItemResultBy.Automatic;

                        StarSystemUpdateQueueItemUpdated starSystemUpdateQueueItemUpdated = new(starSystemUpdateQueueItem.Id);
                        await Producer.SendAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing, starSystemUpdateQueueItemUpdated.Message, cancellationToken);
                    }
                    await DbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    StarSystemThargoidManualUpdate starSystemThargoidManualUpdate = new(model.SystemAddress, null, model.SystemState, model.Progress)
                    {
                        DaysLeft = model.DaysLeft,
                    };
                    await Producer.SendAsync(StarSystemThargoidManualUpdate.QueueName, StarSystemThargoidManualUpdate.Routing, starSystemThargoidManualUpdate.Message, cancellationToken);
                }
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<ActionResult> SystemUpdateFailed([FromBody] SystemUpdateFailedRequest model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                StarSystem? starSystem = await DbContext.StarSystems
                    .Include(s => s.ThargoidLevel)
                    .FirstOrDefaultAsync(s => s.SystemAddress == model.SystemAddress, cancellationToken);
                if (starSystem == null)
                {
                    return NotFound();
                }

                List<StarSystemUpdateQueueItem> starSystemUpdateQueueItems = await DbContext.StarSystemUpdateQueueItems
                    .Where(s => s.StarSystem == starSystem && s.Status == StarSystemUpdateQueueItemStatus.PendingAutomaticReview)
                    .ToListAsync(cancellationToken);
                foreach (StarSystemUpdateQueueItem starSystemUpdateQueueItem in starSystemUpdateQueueItems)
                {
                    starSystemUpdateQueueItem.Status = StarSystemUpdateQueueItemStatus.PendingManualReview;
                    StarSystemUpdateQueueItemUpdated starSystemUpdateQueueItemUpdated = new(starSystemUpdateQueueItem.Id);
                    await Producer.SendAsync(StarSystemUpdateQueueItemUpdated.QueueName, StarSystemUpdateQueueItemUpdated.Routing, starSystemUpdateQueueItemUpdated.Message, cancellationToken);
                }
                await DbContext.SaveChangesAsync(cancellationToken);
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<SystemUpdateRequestedResponse> SystemUpdatePending(CancellationToken cancellationToken)
        {
            List<StarSystemUpdateQueueItem> starSystemUpdateQueueItems = await DbContext.StarSystemUpdateQueueItems
                .AsNoTracking()
                .Include(s => s.StarSystem)
                .Where(s => s.Status == StarSystemUpdateQueueItemStatus.PendingAutomaticReview)
                .Take(25)
                .ToListAsync(cancellationToken);

            return new SystemUpdateRequestedResponse(starSystemUpdateQueueItems
                .Where(s => s.StarSystem != null)
                .Select(s => new SystemUpdateRequestedResponseSystem(s.StarSystem!))
                .ToList());
        }
    }

    public class UpdateModel
    {
        public long SystemAddress { get; set; }

        [EnumDataType(typeof(StarSystemThargoidLevelState))]
        public StarSystemThargoidLevelState SystemState { get; set; }

        [Range(0, 100)]
        public short? Progress { get; set; }

        public short? DaysLeft { get; set; }

        public UpdateModel(long systemAddress, StarSystemThargoidLevelState systemState, short progress, short? daysLeft)
        {
            SystemAddress = systemAddress;
            SystemState = systemState;
            Progress = progress;
            DaysLeft = daysLeft;
        }
    }

    public class SystemUpdateRequestedResponse
    {
        public List<SystemUpdateRequestedResponseSystem> Systems { get; }

        public SystemUpdateRequestedResponse(List<SystemUpdateRequestedResponseSystem> systems)
        {
            Systems = systems;
        }
    }

    public class SystemUpdateRequestedResponseSystem
    {
        public string SystemName { get; set; }
        public long SystemAddress { get; set; }

        public SystemUpdateRequestedResponseSystem(StarSystem starSystem)
        {
            SystemName = starSystem.Name;
            SystemAddress = starSystem.SystemAddress;
        }
    }

    public class SystemUpdateFailedRequest
    {
        public long SystemAddress { get; set; }

        public SystemUpdateFailedRequest(StarSystem starSystem)
        {
            SystemAddress = starSystem.SystemAddress;
        }
    }
}
