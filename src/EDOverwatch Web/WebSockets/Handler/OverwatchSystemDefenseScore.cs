using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.NotTracked;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchSystemDefenseScore : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        public OverwatchSystemDefenseScore()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchSystemDefenseScores.Create(dbContext, cancellationToken), new NotTrackedObject());
        }
    }
}
