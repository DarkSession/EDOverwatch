using ActiveMQ.Artemis.Client;
using EDOverwatch_Web.Models;
using Messages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Home
{
    public class HomeObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing),
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (StarSystemUpdated.QueueName, StarSystemUpdated.Routing),
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
        };

        private IMemoryCache MemoryCache { get; }

        public HomeObjectEvents(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchOverview.DeleteMemoryEntry(MemoryCache);

            HomeObject homeObject = new();
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(homeObject)).ToList();
            if (sessions.Any())
            {
                if (queueName == StarSystemUpdated.QueueName &&
                    json.ToObject<StarSystemUpdated>() is StarSystemUpdated starSystemUpdated &&
                    !await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == starSystemUpdated.SystemAddress && s.WarRelevantSystem, cancellationToken))
                {
                    return;
                }
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchHome), await OverwatchOverview.Create(dbContext, MemoryCache, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
