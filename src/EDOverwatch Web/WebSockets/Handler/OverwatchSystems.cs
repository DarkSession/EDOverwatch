using EDOverwatch_Web.WebSockets.EventListener.Systems;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystems : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchSystems(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchStarSystems.Create(dbContext, AppCache, cancellationToken), new SystemsObject());
        }
    }
}
