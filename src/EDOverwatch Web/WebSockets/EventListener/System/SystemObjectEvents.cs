using ActiveMQ.Artemis.Client;
using Messages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.System
{
    public class SystemObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (StarSystemThargoidLevelChanged.QueueName, StarSystemThargoidLevelChanged.Routing),
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
        };

        private IMemoryCache MemoryCache { get; }

        public SystemObjectEvents(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
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
            }
            if (systemAddress == 0)
            {
                return;
            }

            SystemObject systemObject = new(systemAddress);
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(systemObject)).ToList();
            if (sessions.Any())
            {
                Models.OverwatchStarSystemFullDetail.DeleteMemoryEntry(MemoryCache, systemAddress);
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchSystem), await Models.OverwatchStarSystemFullDetail.Create(systemAddress, dbContext, MemoryCache, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
