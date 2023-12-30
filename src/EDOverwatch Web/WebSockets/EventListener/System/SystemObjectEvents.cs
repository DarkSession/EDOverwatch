using ActiveMQ.Artemis.Client;
using LazyCache;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.System
{
    public class SystemObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
            (StarSystemUpdated.QueueName, StarSystemUpdated.Routing),
        };

        private IAppCache AppCache { get; }

        public SystemObjectEvents(IAppCache appCache)
        {
            AppCache = appCache;
        }

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
                case StarSystemUpdated.QueueName:
                    {
                        StarSystemUpdated? data = json.ToObject<StarSystemUpdated>();
                        systemAddress = data?.SystemAddress ?? 0;
                        break;
                    }
            }
            if (systemAddress == 0)
            {
                return;
            }

            Models.OverwatchStarSystemFullDetail.DeleteMemoryEntry(AppCache, systemAddress);

            SystemObject systemObject = new(systemAddress);
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(systemObject)).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchSystem), await Models.OverwatchStarSystemFullDetail.Create(systemAddress, dbContext, AppCache, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
