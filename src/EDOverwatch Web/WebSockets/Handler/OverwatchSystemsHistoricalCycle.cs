using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.NotTracked;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystemsHistoricalCycle : WebSocketHandler
    {
        class OverwatchSystemsHistoricalCycleRequest
        {
            public DateOnly? Cycle { get; set; }
        }

        protected override Type? MessageDataType => typeof(OverwatchSystemsHistoricalCycleRequest);

        public override bool AllowAnonymous => true;

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemsHistoricalCycleRequest? data = message.Data?.ToObject<OverwatchSystemsHistoricalCycleRequest>();
            if (data != null)
            {
                DateOnly cycle = data.Cycle ?? DateOnly.FromDateTime(DateTime.UtcNow);
                if (cycle > DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date) || !await dbContext.ThargoidCycleExists(cycle, cancellationToken))
                {
                    cycle = DateOnly.FromDateTime(WeeklyTick.GetTickTime(DateTimeOffset.UtcNow).DateTime);
                }
                OverwatchStarSystemsHistorical result = await OverwatchStarSystemsHistorical.Create(cycle, dbContext, cancellationToken);
                return new WebSocketHandlerResultSuccess(result, new NotTrackedObject());
            }
            return new WebSocketHandlerResultError();
        }
    }
}
