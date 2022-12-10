namespace EDOverwatch_Web.WebSockets.EventListener.Systems
{
    public class SystemsObject : WebSocketSessionActiveObject
    {
        public SystemsObject() : base("Systems")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject) => Name == webSocketSessionActiveObject.Name;
    }
}
