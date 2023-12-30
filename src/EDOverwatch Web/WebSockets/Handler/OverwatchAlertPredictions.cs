using EDOverwatch_Web.WebSockets.EventListener.Home;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchAlertPredictions : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchAlertPredictions(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchAlertPredictions.Create(dbContext, AppCache, cancellationToken), new HomeObject());
        }
    }
}
