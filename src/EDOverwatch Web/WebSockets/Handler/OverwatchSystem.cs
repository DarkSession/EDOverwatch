using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.System;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystem : WebSocketHandler
    {
        class OverwatchSystemRequest
        {
            public long SystemAddress { get; set; }
        }

        protected override Type? MessageDataType => typeof(OverwatchSystemRequest);

        public override bool AllowAnonymous => true;

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemRequest? data = message.Data?.ToObject<OverwatchSystemRequest>();
            if (data != null)
            {
                OverwatchStarSystemDetail? overwatchStarSystemDetail = await OverwatchStarSystemDetail.Create(data.SystemAddress, dbContext, cancellationToken);
                if (overwatchStarSystemDetail != null)
                {
                    return new WebSocketHandlerResultSuccess(overwatchStarSystemDetail, new SystemObject(data.SystemAddress));
                }
            }
            return new WebSocketHandlerResultError();
        }
    }
}
