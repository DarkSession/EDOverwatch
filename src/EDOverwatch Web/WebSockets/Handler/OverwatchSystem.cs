using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.System;
using Microsoft.Extensions.Caching.Memory;

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

        private IMemoryCache MemoryCache { get; }

        public OverwatchSystem(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchSystemRequest? data = message.Data?.ToObject<OverwatchSystemRequest>();
            if (data != null)
            {
                OverwatchStarSystemFullDetail? overwatchStarSystemDetail = await OverwatchStarSystemFullDetail.Create(data.SystemAddress, dbContext, MemoryCache, cancellationToken);
                if (overwatchStarSystemDetail != null)
                {
                    return new WebSocketHandlerResultSuccess(overwatchStarSystemDetail, new SystemObject(data.SystemAddress));
                }
            }
            return new WebSocketHandlerResultError();
        }
    }
}
