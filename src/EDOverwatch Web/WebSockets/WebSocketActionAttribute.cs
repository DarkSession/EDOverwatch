namespace EDOverwatch_Web.WebSockets
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WebSocketActionAttribute : Attribute
    {
        public WebSocketAction Action { get; }
        public WebSocketActionAttribute(WebSocketAction action)
        {
            Action = action;
        }
    }

    public interface IWebSocketAction
    {
        public ValueTask Process(WebSocketSession webSocketSession, CancellationToken cancellationToken);
    }

    public enum WebSocketAction : byte
    {
        OnUserConnected,
        OnUserDisconnected,
    }
}
