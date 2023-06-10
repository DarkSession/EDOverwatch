namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderApiKeyGenerate : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        class CommanderApiKeyGenerateResponse
        {
            public Guid ApiKey { get; }

            public CommanderApiKeyGenerateResponse(Guid apiKey)
            {
                ApiKey = apiKey;
            }
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            if (user is null)
            {
                throw new Exception("user is null");
            }
            await dbContext.Entry(user)
                .Reference(u => u.Commander)
                .Query()
                .Include(c => c.ApiKey)
                .LoadAsync(cancellationToken);
            if (user.Commander is null)
            {
                throw new Exception("commander is null");
            }
            if (user.Commander.ApiKey == null)
            {
                user.Commander.ApiKey = new(0, Guid.NewGuid(), DateTimeOffset.UtcNow, CommanderApiKeyStatus.Active);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            CommanderApiKeyGenerateResponse response = new(user.Commander.ApiKey.Key);
            return new WebSocketHandlerResultSuccess(response, null);
        }
    }
}
