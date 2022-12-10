using System.Net.WebSockets;

namespace EDOverwatch_Web.WebSockets
{
    public class WebSocketSession
    {
        public WebSocket WebSocket { get; }
        public string? UserId { get; }
        public WebSocketSessionActiveObject ActiveObject { get; set; } = new WebSocketSessionActiveObjectNone();

        public WebSocketSession(WebSocket webSocket, ApplicationUser? user)
        {
            WebSocket = webSocket;
            UserId = user?.Id;
        }
    }
}
