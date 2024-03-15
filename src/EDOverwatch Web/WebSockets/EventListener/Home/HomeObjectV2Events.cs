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
        private DateTimeOffset LastFullUpdate { get; set; }

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
                long systemAddress;
                if (queueName == StarSystemUpdated.QueueName &&
                    json.ToObject<StarSystemUpdated>() is StarSystemUpdated starSystemUpdated)
                {
                    systemAddress = starSystemUpdated.SystemAddress;
                    if (!await dbContext.StarSystems.AnyAsync(s => s.SystemAddress == systemAddress && s.ThargoidLevel != null && s.ThargoidLevel.State > StarSystemThargoidLevelState.None, cancellationToken))
                    {
                        return;
                    }
                }
                else if (queueName == StarSystemThargoidLevelChanged.QueueName &&
                    json.ToObject<StarSystemThargoidLevelChanged>() is StarSystemThargoidLevelChanged starSystemThargoidLevelChanged)
                {
                    if (!starSystemThargoidLevelChanged.Changed)
                    {
                        return;
                    }
                    systemAddress = starSystemThargoidLevelChanged.SystemAddress;
                }
                else
                {
                    return;
                }

                WebSocketMessage webSocketMessage;
                if ((DateTimeOffset.UtcNow - LastFullUpdate).TotalMinutes >= 5)
                {
                    LastFullUpdate = DateTimeOffset.UtcNow;
                    webSocketMessage = new(nameof(Handler.OverwatchHomeV2), await OverwatchOverviewV2.Create(dbContext, AppCache, EdMaintenance, cancellationToken));
                }
                else
                {
                    OverwatchOverviewV2PartialUpdate? data = await OverwatchOverviewV2PartialUpdate.Create(systemAddress, dbContext, AppCache, EdMaintenance, cancellationToken);
                    if (data is null)
                    {
                        return;
                    }
                    webSocketMessage = new("OverwatchHomeV2PartialUpdate", data);
                }
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
