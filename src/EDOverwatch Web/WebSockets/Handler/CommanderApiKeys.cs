namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderApiKeys : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public CommanderApiKeys()
        {
        }

        class CommanderApiKeysResponse
        {
            public Guid? ApiKey { get; }
            public List<Guid> AdditionalKeys { get; }

            public CommanderApiKeysResponse(Guid? apiKey, List<Guid> additionalKeys)
            {
                ApiKey = apiKey;
                AdditionalKeys = additionalKeys;
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
                .AsSplitQuery()
                .Include(c => c.ApiKey)
                .Include(c => c.ApiKeyClaims!)
                .ThenInclude(a => a.ApiKey)
                .LoadAsync(cancellationToken);
            if (user.Commander is null)
            {
                throw new Exception("commander is null");
            }
            List<Guid> additionalKeys = new();
            if (user.Commander.ApiKeyClaims!.Any())
            {
                additionalKeys.AddRange(user.Commander.ApiKeyClaims!.Where(a => a.ApiKey != null).Select(a => a.ApiKey!.Key));
            }

            CommanderApiKeysResponse response = new(user.Commander.ApiKey?.Key, additionalKeys);
            return new WebSocketHandlerResultSuccess(response, null);
        }
    }
}
