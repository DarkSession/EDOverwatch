namespace EDOverwatch_Web.WebSockets.EventListener.Maelstrom
{
    public class MaelstromObject : WebSocketSessionActiveObject
    {
        public string MaelstromName { get; }
        public MaelstromObject(string maelstromName) : base("Maelstrom")
        {
            MaelstromName = maelstromName;
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return
                webSocketSessionActiveObject.Name == Name && webSocketSessionActiveObject is MaelstromObject maelstromObject &&
                maelstromObject.MaelstromName == MaelstromName;
        }
    }
}
