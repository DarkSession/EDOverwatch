namespace EDOverwatch_Web.WebSockets
{
    public abstract class WebSocketSessionActiveObject
    {
        public string Name { get; }
        protected WebSocketSessionActiveObject(string name)
        {
            Name = name;
        }

        public abstract bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject);
    }

    public class WebSocketSessionActiveObjectNone : WebSocketSessionActiveObject
    {
        public WebSocketSessionActiveObjectNone() : base("none")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject) => false;
    }
}
