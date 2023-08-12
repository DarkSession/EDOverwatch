using EDOverwatch_Web.WebSockets.EventListener.Maelstroms;
using Microsoft.Extensions.Caching.Memory;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchMaelstroms : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IMemoryCache MemoryCache { get; }

        public OverwatchMaelstroms(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            Models.OverwatchMaelstroms overwatchMaelstroms = await Models.OverwatchMaelstroms.Create(dbContext, MemoryCache, cancellationToken);
            return new WebSocketHandlerResultSuccess(overwatchMaelstroms, new MaelstromsObject());
        }
    }
}
