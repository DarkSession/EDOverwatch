namespace EDOverwatch_Web.WebSockets.EventListener.NotTracked
{
    public class NotTrackedObject : WebSocketSessionActiveObject
    {
        public NotTrackedObject() : base("NotTracked")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return webSocketSessionActiveObject.Name == Name;
        }
    }
}
