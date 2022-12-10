using EDOverwatch_Web.WebSockets.EventListener.CommanderWarEfforts;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderWarEfforts : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public CommanderWarEfforts()
        {
        }

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
            return new WebSocketHandlerResultSuccess(await Models.CommanderWarEffort.Create(dbContext, user, cancellationToken), new CommanderWarEffortsObject(user.Commander.Id));
        }
    }
}
