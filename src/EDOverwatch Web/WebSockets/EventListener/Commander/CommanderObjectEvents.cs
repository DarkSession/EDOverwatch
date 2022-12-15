using ActiveMQ.Artemis.Client;
using Messages;
using Newtonsoft.Json.Linq;

namespace EDOverwatch_Web.WebSockets.EventListener.Commander
{
    public class CommanderObjectEvents : IEventListener
    {
        public List<(string queueName, RoutingType routingType)> Events { get; } = new()
        {
            (CommanderCApi.QueueName, CommanderCApi.Routing),
        };

        public async ValueTask ProcessEvent(string queueName, JObject json, WebSocketServer webSocketServer, EdDbContext dbContext, CancellationToken cancellationToken)
        {
            CommanderCApi? commanderCApi = json.ToObject<CommanderCApi>();
            if (commanderCApi == null ||
                await dbContext.Commanders
                    .Include(c => c.User)
                    .SingleOrDefaultAsync(c => c.FDevCustomerId == commanderCApi.FDevCustomerId, cancellationToken) is not EDDatabase.Commander commander ||
                commander.User == null)
            {
                return;
            }
            List<WebSocketSession> sessions = webSocketServer.ActiveSessions.Where(a => a.UserId == commander.User.Id).ToList();
            if (sessions.Any())
            {
                WebSocketMessage webSocketMessage = new(nameof(Handler.CommanderMe), new Models.User(commander.Name ?? commander.User.UserName ?? "Unknown", commander.JournalLastActivity));
                foreach (WebSocketSession session in sessions)
                {
                    await webSocketMessage.Send(session, cancellationToken);
                }
            }
        }
    }
}
