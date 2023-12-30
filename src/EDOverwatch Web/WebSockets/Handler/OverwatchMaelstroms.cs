using EDOverwatch_Web.WebSockets.EventListener.Maelstroms;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchMaelstroms : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchMaelstroms(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            Models.OverwatchMaelstroms overwatchMaelstroms = await Models.OverwatchMaelstroms.Create(dbContext, AppCache, cancellationToken);
            return new WebSocketHandlerResultSuccess(overwatchMaelstroms, new MaelstromsObject());
        }
    }
}
