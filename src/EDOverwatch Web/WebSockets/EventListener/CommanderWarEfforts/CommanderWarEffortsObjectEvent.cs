using ActiveMQ.Artemis.Client;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.CommanderWarEfforts
{
    public class CommanderWarEffortsObjectEvent : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (WarEffortUpdated.QueueName, WarEffortUpdated.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            WarEffortUpdated? warEffortUpdated = json.ToObject<WarEffortUpdated>();
            if (warEffortUpdated == null ||
                warEffortUpdated.FDevCustomerId != null ||
                await dbContext.Commanders.SingleOrDefaultAsync(c => c.FDevCustomerId == warEffortUpdated.FDevCustomerId, cancellationToken) is not EDDatabase.Commander commander)
            {
                return;
            }
            CommanderWarEffortsObject commanderWarEffortsObject = new(commander.Id);
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.ActiveObject.IsActiveObject(commanderWarEffortsObject)).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.CommanderWarEfforts), await Models.CommanderWarEffort.Create(dbContext, commander, cancellationToken));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
