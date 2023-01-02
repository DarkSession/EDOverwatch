using ActiveMQ.Artemis.Client;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Commander
{
    public class CommanderObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (CommanderUpdated.QueueName, CommanderUpdated.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            CommanderUpdated? commanderUpdated = json.ToObject<CommanderUpdated>();
            if (commanderUpdated == null ||
                await dbContext.Commanders
                    .Include(c => c.User)
                    .SingleOrDefaultAsync(c => c.FDevCustomerId == commanderUpdated.FDevCustomerId, cancellationToken) is not EDDatabase.Commander commander ||
                commander.User == null)
            {
                return;
            }
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.UserId == commander.User.Id).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.CommanderMe), new Models.User(commander));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
