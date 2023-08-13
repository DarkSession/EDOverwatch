namespace EDOverwatch_Web.WebSockets.EventListener.CommanderWarEfforts
{
    public class CommanderWarEffortsV3Object : WebSocketSessionActiveObject
    {
        public int CommanderId { get; set; }

        public CommanderWarEffortsV3Object(int commanderId) : base("CommanderWarEffortsV3")
        {
            CommanderId = commanderId;
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return
                webSocketSessionActiveObject.Name == Name && webSocketSessionActiveObject is CommanderWarEffortsV3Object commanderWarEffortsV3Object &&
                commanderWarEffortsV3Object.CommanderId == CommanderId;
        }
    }
}
