using EDOverwatch_Web.WebSockets;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.sWebSocket.EventListener
{
    public interface IEventListener
    {
        public List<string> Events { get; }

        public ValueTask ProcessEvent(JObject json, WebSocketServer webSocketServer, EdDbContext dbContext);
    }
}
