using Messages;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderCApiImportRequest : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        private ActiveMqMessageProducer Producer { get; }

        public CommanderCApiImportRequest(ActiveMqMessageProducer producer)
        {
            Producer = producer;
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
            else if (!user.Commander.CanProcessCApiJournal)
            {
                return new WebSocketHandlerResultSuccess(new CommanderCApiImportRequestResponse(false, "Import request rejected"), null);
            }

            CommanderCApi commanderCApi = new(user.Commander.FDevCustomerId);
            await Producer.SendAsync(CommanderCApi.QueueName, CommanderCApi.Routing, commanderCApi.Message, cancellationToken);

            return new WebSocketHandlerResultSuccess(new CommanderCApiImportRequestResponse(true, "Import requested"), null);
        }
    }

    public class CommanderCApiImportRequestResponse
    {
        public bool Requested { get; }
        public string? Message { get; }

        public CommanderCApiImportRequestResponse(bool requested, string? message = null)
        {
            Requested = requested;
            Message = message;
        }
    }
}
