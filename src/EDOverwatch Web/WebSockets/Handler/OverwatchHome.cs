﻿using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.Home;
using LazyCache;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchHome : WebSocketHandler
    {
        protected override Type? MessageDataType => null;

        public override bool AllowAnonymous => true;

        private IAppCache AppCache { get; }

        public OverwatchHome(IAppCache appCache)
        {
            AppCache = appCache;
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            return new WebSocketHandlerResultSuccess(await OverwatchOverview.Create(dbContext, AppCache, cancellationToken), new HomeObject());
        }
    }
}
