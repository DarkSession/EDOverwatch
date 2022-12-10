namespace EDOverwatch_Web.WebSockets.EventListener.CommanderWarEfforts
{
    public class CommanderWarEffortsObject : WebSocketSessionActiveObject
    {
        public int CommanderId { get; set; }

        public CommanderWarEffortsObject(int commanderId) : base("CommanderWarEfforts")
        {
            CommanderId = commanderId;
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject)
        {
            return
                webSocketSessionActiveObject.Name == Name && webSocketSessionActiveObject is CommanderWarEffortsObject commanderWarEffortsObject &&
                commanderWarEffortsObject.CommanderId == CommanderId;
        }
    }
}
