namespace EDOverwatch_Web.WebSockets.EventListener.Maelstroms
{
    public class MaelstromsObject : WebSocketSessionActiveObject
    {
        public MaelstromsObject() : base("Maelstroms")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return webSocketSessionActiveObject.Name == Name && webSocketSessionActiveObject is MaelstromsObject;
        }
    }
}
