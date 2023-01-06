using EDOverwatch_Web.Models;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchThargoidCycles : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        public OverwatchThargoidCycles()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchThargoidCycle.GetThargoidCycles(dbContext, cancellationToken), null);
        }
    }
}
