using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EDOverwatch_Web.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "ApiKeyAuthentication")]
    public class ProgressUpdateController : ControllerBase
    {
        private EdDbContext DbContext { get; }
        private ActiveMqMessageProducer Producer { get; }
        public ProgressUpdateController(EdDbContext dbContext, ActiveMqMessageProducer producer)
        {
            DbContext = dbContext;
            Producer = producer;
        }

        public async Task<ActionResult> Post([FromBody] ProgressUpdateModel model, CancellationToken cancellationToken)
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
    }

    public class ProgressUpdateModel
    {
        public long SystemAddress { get; set; }

        [EnumDataType(typeof(StarSystemThargoidLevelState))]
        public StarSystemThargoidLevelState SystemState { get; set; }

        [Range(0, 100)]
        public short Progress { get; set; }

        public short? DaysLeft { get; set; }

        public ProgressUpdateModel(long systemAddress, StarSystemThargoidLevelState systemState, short progress, short? daysLeft)
        {
            SystemAddress = systemAddress;
            SystemState = systemState;
            Progress = progress;
            DaysLeft = daysLeft;
        }
    }
}
