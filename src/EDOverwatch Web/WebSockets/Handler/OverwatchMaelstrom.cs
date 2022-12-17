using EDOverwatch_Web.Models;
using EDOverwatch_Web.WebSockets.EventListener.Maelstrom;

namespace EDOverwatch_Web.WebSockets.Handler
{
    public class OverwatchMaelstrom : WebSocketHandler
    {
        class OverwatchMaelstromRequest
        {
            public string Name { get; }

            public OverwatchMaelstromRequest(string name)
            {
                Name = name;
            }
        }

        protected override Type? MessageDataType => typeof(OverwatchMaelstromRequest);

        public override bool AllowAnonymous => true;

        public OverwatchMaelstrom()
        {
        }

        public override async ValueTask<WebSocketHandlerResult> ProcessMessage(WebSocketMessageReceived message, WebSocketSession webSocketSession, ApplicationUser? user, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            OverwatchMaelstromRequest? data = message.Data?.ToObject<OverwatchMaelstromRequest>();
            if (data != null)
            {
                ThargoidMaelstrom? maelstrom = await dbContext.ThargoidMaelstroms.FirstOrDefaultAsync(t => t.Name == data.Name, cancellationToken);
                if (maelstrom != null)
                {
                    OverwatchMaelstromDetail? overwatchMaelstromDetail = await OverwatchMaelstromDetail.Create(maelstrom, dbContext, cancellationToken);
                    if (overwatchMaelstromDetail != null)
                    {
                        return new WebSocketHandlerResultSuccess(overwatchMaelstromDetail, new MaelstromObject(data.Name));
                    }
                }
            }
            return new WebSocketHandlerResultError();
        }
    }
}
