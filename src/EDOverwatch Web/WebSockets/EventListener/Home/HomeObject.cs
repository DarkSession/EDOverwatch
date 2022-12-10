﻿namespace EDOverwatch_Web.WebSockets.EventListener.Home
{
    public class HomeObject : WebSocketSessionActiveObject
    {
        public HomeObject() : base("Home")
        {
        }

        public override bool IsActiveObject(WebSocketSessionActiveObject webSocketSessionActiveObject) => Name == webSocketSessionActiveObject.Name;
    }
}
