namespace EDOverwatch_Web.WebSockets.EventListener.Home
{
    public class HomeV2Object : WebSocketSessionActiveObject
    {
        public HomeV2Object() : base("HomeV2")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject) => Name == webSocketSessionActiveObject.Name;
    }
}
