using ActiveMQ.Artemis.Client;
using EDOverwatch_Web.Models;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Maelstrom
{
    public class MaelstromObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            long systemAddress = 0;
            switch (queueName)
            {
                case StarSystemThargoidLevelChanged.QueueName:
                    {
                        StarSystemThargoidLevelChanged? data = json.ToObject<StarSystemThargoidLevelChanged>();
                        systemAddress = data?.SystemAddress ?? 0;
                        break;
                    }
                case WarEffortUpdated.QueueName:
                    {
                        WarEffortUpdated? data = json.ToObject<WarEffortUpdated>();
                        systemAddress = data?.SystemAddress ?? 0;
                        break;
                    }
            }
            if (systemAddress == 0)
            {
                return;
            }

            StarSystem? starSystem = await dbContext.StarSystems
                .Include(s => s.ThargoidLevel)
                .ThenInclude(t => t!.Maelstrom)
                .FirstOrDefaultAsync(s => s.SystemAddress == systemAddress, cancellationToken);
            if (starSystem?.ThargoidLevel?.Maelstrom == null)
            {
                return;
            }

            MaelstromObject maelstromObject = new(starSystem.ThargoidLevel.Maelstrom.Name);
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(maelstromObject)).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchMaelstrom), await OverwatchMaelstromDetail.Create(starSystem.ThargoidLevel.Maelstrom, dbContext, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
