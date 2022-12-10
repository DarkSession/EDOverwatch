using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets;
using EDOverwatch_Web.WebSockets.EventListener.Home;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.sWebSocket.EventListener.Home
{
    public class HomeObjectEvents : IEventListener
    {
        public List<string> Events { get; } = new()
        {
            "ThargoidMaelstrom.CreatedUpdated",
        };

        public async ValueTask ProcessEvent(JObject json, WebSocketServer webSocketServer, EdDbContext dbContext)
        {
            HomeObject homeObject = new();
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(homeObject)).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new("OverwatchHome", await OverwatchOverview.LoadOverwatchOverview(dbContext, CancellationToken.None));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, CancellationToken.None);
                }
            }
        }
    }
}
