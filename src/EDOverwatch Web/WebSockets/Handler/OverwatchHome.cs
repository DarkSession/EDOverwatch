using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.Home;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchHome : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public OverwatchHome()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchOverview.LoadOverwatchOverview(dbContext, cancellationToken), new HomeObject());
        }
    }
}
