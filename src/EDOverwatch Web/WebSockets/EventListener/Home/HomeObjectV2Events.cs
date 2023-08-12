using ActiveMQ.Artemis.Client;
using EDOverwatch_Web.Models;
using Messages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Home
{
    public class HomeObjectV2Events : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (ThargoidMaelstromCreatedUpdated.QueueName, ThargoidMaelstromCreatedUpdated.Routing),
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (StarSystemUpdated.QueueName, StarSystemUpdated.Routing),
        };

        private IMemoryCache MemoryCache { get; }

        public HomeObjectV2Events(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            HomeV2Object homeObject = new();
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(homeObject)).ToList();
            if (sessions.Any())
            {
                if (queueName == StarSystemUpdated.QueueName &&
                    json.ToObject<StarSystemUpdated>() is StarSystemUpdated starSystemUpdated &&
                    !await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == starSystemUpdated.SystemAddress && s.WarRelevantSystem, cancellationToken))
                {
                    return;
                }
                OverwatchOverviewV2.DeleteMemoryEntry(MemoryCache);
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchHomeV2), await OverwatchOverviewV2.Create(dbContext, MemoryCache, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
