using ActiveMQ.Artemis.Client;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Systems
{
    public class SystemsObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing),
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            SystemsObject systemsObject = new();
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(systemsObject)).ToList();
            if (sessions.Any())
            {
                if (queueName == StarSystemUpdated.QueueName &&
                    json.ToObject<StarSystemUpdated>() is StarSystemUpdated starSystemUpdated &&
                    !await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == starSystemUpdated.SystemAddress && s.WarRelevantSystem, cancellationToken))
                {
                    return;
                }
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchSystems), await Models.OverwatchStarSystems.Create(dbContext, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
