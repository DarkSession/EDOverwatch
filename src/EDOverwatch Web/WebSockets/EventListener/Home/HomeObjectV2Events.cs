using ActiveMQ.Artemis.Client;
using EDOverwatch_Web.Models;
using EDOverwatch_Web.Services;
using LazyCache;
using Messages;
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

        private IAppCache AppCache { get; }
        private EdMaintenance EdMaintenance { get; }

        public HomeObjectV2Events(IAppCache appCache, EdMaintenance edMaintenance)
        {
            AppCache = appCache;
            EdMaintenance = edMaintenance;
        }

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchOverviewV2.DeleteMemoryEntry(AppCache);

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
                if (queueName == StarSystemThargoidLevelChanged.QueueName &&
                    json.ToObject<StarSystemThargoidLevelChanged>() is StarSystemThargoidLevelChanged starSystemThargoidLevelChanged &&
                    !starSystemThargoidLevelChanged.Changed)
                {
                    return;
                }
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchHomeV2), await OverwatchOverviewV2.Create(dbContext, AppCache, EdMaintenance, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
