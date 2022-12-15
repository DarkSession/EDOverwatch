namespace EDOverwatch_Web.WebSockets.EventListener.System
{
    public class SystemObject : WebSocketSessionActiveObject
    {
        public long SystemAddress { get; }

        public SystemObject(long systemAddress) : base("System")
        {
            SystemAddress = systemAddress;
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return
                webSocketSessionActiveObject.Name == Name && webSocketSessionActiveObject is SystemObject systemObject &&
                systemObject.SystemAddress == SystemAddress;
        }
    }
}
