using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.NotTracked;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystemAnalysis : WebSocketHandler
    {
        class OverwatchSystemAnalysisRequest
        {
            public long SystemAddress { get; set; }
            public DateOnly Cycle { get; set; }
        }

        protected override Type? MessageDataType => typeof(OverwatchSystemAnalysisRequest);

        public override bool AllowAnonymous => true;

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemAnalysisRequest? data = message.Data?.ToObject<OverwatchSystemAnalysisRequest>();
            if (data != null)
            {
                OverwatchStarSystemCycleAnalysis? overwatchStarSystemCycleAnalysis = await OverwatchStarSystemCycleAnalysis.Create(data.SystemAddress, data.Cycle, dbContext, cancellationToken);
                if (overwatchStarSystemCycleAnalysis != null)
                {
                    return new WebSocketHandlerResultSuccess(overwatchStarSystemCycleAnalysis, new NotTrackedObject());
                }
            }
            return new WebSocketHandlerResultError();
        }
    }
}
