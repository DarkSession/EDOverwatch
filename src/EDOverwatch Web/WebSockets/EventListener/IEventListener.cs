using ActiveMQ.Artemis.Client;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener
{
    public interface IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; }

        public ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken);
    }
}
