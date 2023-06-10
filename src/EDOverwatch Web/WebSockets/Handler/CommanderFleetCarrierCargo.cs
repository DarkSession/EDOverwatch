namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderFleetCarrierCargo : WebSocketHandler
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

            return new WebSocketHandlerResultSuccess(Models.CommanderFleetCarrierCargo.Create(dbContext, user.Commander, cancellationToken), null);
        }
    }
}
