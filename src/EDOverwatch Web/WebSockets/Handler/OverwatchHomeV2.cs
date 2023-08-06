using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.Home;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchHomeV2 : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchOverviewV2.Create(dbContext, cancellationToken), new HomeObject());
        }
    }
}
