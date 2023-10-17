﻿using ActiveMQ.Artemis.Client;
using Messages;
using Microsoft.Extensions.Caching.Memory;
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

        private IMemoryCache MemoryCache { get; }

        public SystemsObjectEvents(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
        }

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            Models.OverwatchStarSystems.DeleteMemoryEntry(MemoryCache);

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
                WebSocketMessage webSocketMessage = new(nameof(Handler.OverwatchSystems), await Models.OverwatchStarSystems.Create(dbContext, MemoryCache, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
