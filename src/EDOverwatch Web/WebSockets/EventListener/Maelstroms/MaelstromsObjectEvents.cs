using ActiveMQ.Artemis.Client;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Maelstroms
{
    public class MaelstromsObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            MaelstromsObject maelstromObject = new();
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(maelstromObject)).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchMaelstroms), await Models.OverwatchMaelstroms.Create(dbContext, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}