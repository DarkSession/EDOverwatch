using Messages;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class AdminSystemUpdate : WebSocketHandler
    {
        protected override Type? MessageDataType => typeof(AdminSystemUpdateRequest);

        class AdminSystemUpdateRequest
        {
            public long SystemAddress { get; set; }
            public bool IsCounterstrikeSystem { get; set; }

            public AdminSystemUpdateRequest(long systemAddress)
            {
                SystemAddress = systemAddress;
            }
        }

        class AdminSystemUpdateResponse
        {
            public bool Success { get; }

            public AdminSystemUpdateResponse(bool success)
            {
                Success = success;
            }
        }

        private ActiveMqMessageProducer Producer { get; }

        public AdminSystemUpdate(ActiveMqMessageProducer producer)
        {
            Producer = producer;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            AdminSystemUpdateRequest? data = message.Data?.ToObject<AdminSystemUpdateRequest>();
            if (data == null || user == null)
            {
                return new WebSocketHandlerResultError("Invalid request.");
            }
            await dbContext.Entry(user)
                .Reference(u => u.Commander)
                .LoadAsync(cancellationToken);
            if (user.Commander is null || user.Commander.Permissions != CommanderPermissions.Extra)
            {
                return new WebSocketHandlerResultError("Unauthorized request.");
            }

            StarSystem? starSystem = await dbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .FirstOrDefaultAsync(s => s.SystemAddress == data.SystemAddress, cancellationToken);
            if (starSystem?.ThargoidLevel == null)
            {
                return new WebSocketHandlerResultError("System not found.");
            }
            if (starSystem.ThargoidLevel.State == StarSystemThargoidLevelState.Controlled)
            {
                if (starSystem.ThargoidLevel.IsCounterstrike != data.IsCounterstrikeSystem)
                {
                    starSystem.ThargoidLevel.IsCounterstrike = data.IsCounterstrikeSystem;
                    await dbContext.SaveChangesAsync(cancellationToken);

                    StarSystemThargoidLevelChanged starSystemThargoidLevelChanged = new(starSystem.SystemAddress, true);
                    await Producer.SendAsync(StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing, starSystemThargoidLevelChanged.Message, cancellationToken);
                }
            }
            AdminSystemUpdateResponse response = new(true);
            return new WebSocketHandlerResultSuccess(response, null);
        }
    }
}
