using EDOverwatch_Web.WebSockets.EventListener.NotTracked;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSpireSites : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchSpireSites(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchSpireSites.Create(dbContext, AppCache, cancellationToken), new NotTrackedObject());
        }
    }
}
