using EDOverwatch_Web.WebSockets.EventListener.NotTracked;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchWarStats : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        public OverwatchWarStats()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchWarStats.Create(dbContext, cancellationToken), new NotTrackedObject());
        }
    }
}