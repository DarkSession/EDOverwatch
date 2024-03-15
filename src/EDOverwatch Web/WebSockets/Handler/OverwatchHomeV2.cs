using EDOverwatch_Web.Models;
using EDOverwatch_Web.Services;
using EDOverwatch_Web.WebSockets.EventListener.Home;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchHomeV2 : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }
        private EdMaintenance EdMaintenance { get; }

        public OverwatchHomeV2(IAppCache appCache, EdMaintenance edMaintenance)
        {
            AppCache = appCache;
            EdMaintenance = edMaintenance;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchOverviewV2.Create(dbContext, AppCache, EdMaintenance, cancellationToken), new HomeV2Object());
        }
    }
}
