using EDOverwatch_Web.WebSockets.EventListener.CommanderWarEfforts;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderWarEffortsV3 : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (user is null)
            {
                throw new Exception("user is null");
            }
            await dbContext.Entry(user)
                .Reference(u => u.Commander)
                .LoadAsync(cancellationToken);
            if (user.Commander is null)
            {
                throw new Exception("commander is null");
            }
            Models.CommanderWarEffortsV3? commanderWarEfforts = await Models.CommanderWarEffortsV3.Create(dbContext, user, cancellationToken);
            if (commanderWarEfforts != null)
            {
                return new WebSocketHandlerResultSuccess(commanderWarEfforts, new CommanderWarEffortsV3Object(user.Commander.Id));
            }
            throw new Exception("commanderWarEfforts is null");
        }
    }
}
