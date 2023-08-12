using EDOverwatch_Web.WebSockets.EventListener.NotTracked;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchWarStats : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IMemoryCache MemoryCache { get; }

        public OverwatchWarStats(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchWarStats.Create(dbContext, MemoryCache, cancellationToken), new NotTrackedObject());
        }
    }
}