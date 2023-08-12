using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.Home;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchHomeV2 : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IMemoryCache MemoryCache { get; }

        public OverwatchHomeV2(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchOverviewV2.Create(dbContext, MemoryCache, cancellationToken), new HomeV2Object());
        }
    }
}
