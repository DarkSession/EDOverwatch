using EDOverwatch_Web.WebSockets.EventListener.Systems;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystems : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        public OverwatchSystems()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await Models.OverwatchStarSystems.Create(dbContext, cancellationToken), new SystemsObject());
        }
    }
}
