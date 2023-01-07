namespace EDOverwatch_Web.WebSockets.Handler
{
    public class CommanderApiKeyClaim : WebSocketHandler
    {
        protected override Type? MessageDataType => typeof(CommanderApiKeyClaimRequest);

        public CommanderApiKeyClaim()
        {
        }

        class CommanderApiKeyClaimRequest
        {
            public string Key { get; set; }
            public CommanderApiKeyClaimRequest(string key)
            {
                Key = key;
            }
        }

        class CommanderApiKeyClaimResponse
        {
            public bool Success { get; }

            public CommanderApiKeyClaimResponse(bool success)
            {
                Success = success;
            }
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            CommanderApiKeyClaimRequest? data = message.Data?.ToObject<CommanderApiKeyClaimRequest>();
            if (data != null && Guid.TryParse(data.Key, out Guid key))
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
                CommanderApiKey? commanderApiKey = await dbContext.CommanderApiKeys.SingleOrDefaultAsync(a => a.Key == key, cancellationToken);
                if (commanderApiKey == null)
                {
                    return new WebSocketHandlerResultError("API key not found!");
                }
                else if (await dbContext.CommanderApiKeyClaims.AnyAsync(c => c.ApiKey == commanderApiKey, cancellationToken))
                {
                    return new WebSocketHandlerResultError("API key has already been claimed.");
                }
                else if (await dbContext.Commanders.AnyAsync(c => c.ApiKey == commanderApiKey && c.FDevCustomerId != 0, cancellationToken))
                {
                    return new WebSocketHandlerResultError("API key is associated to a commander with a login and therefore cannot be claimed.");
                }
                dbContext.CommanderApiKeyClaims.Add(new(0)
                {
                    ApiKey = commanderApiKey,
                });
                await dbContext.SaveChangesAsync(cancellationToken);
                CommanderApiKeyClaimResponse response = new(true);
                return new WebSocketHandlerResultSuccess(response, null);
            }
            return new WebSocketHandlerResultError("Invalid API key.");
        }
    }
}
