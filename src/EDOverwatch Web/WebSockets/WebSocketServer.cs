using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Transactions;
using EDOverwatch_Web.WebSockets.EventListener;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;
using System.Net.WebSockets;
using System.Text;

namespace EDOverwatch_Web.WebSockets
{
    public class WebSocketServer
    {
        private ILogger Log { get; }
        private List<WebSocketSession> WebSocketSessions { get; } = new();
        private Dictionary<string, Type> WebSocketHandlers { get; } = new();
        private Dictionary<WebSocketAction, List<Type>> WebSocketActions { get; } = new();
        private JsonSchema WebSocketMessageReceivedSchema { get; } = JsonSchema.FromType<WebSocketMessageReceived>();
        private IConfiguration Configuration { get; }
        private IServiceProvider ServiceProvider { get; }
        private AuthenticationStatusAnnouncement? Announcement { get; }

        public WebSocketServer(ILogger<WebSocketServer> log, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            Log = log;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
            {
                foreach (Type type in GetType().Assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(WebSocketHandler))))
                {
                    WebSocketHandlers[type.Name] = type;
                }
            }
            {
                foreach (Type type in GetType().Assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsClass && typeof(IWebSocketAction).IsAssignableFrom(t)))
                {
                    WebSocketActionAttribute? webSocketActionAttribute = (WebSocketActionAttribute?)type.GetCustomAttributes(typeof(WebSocketActionAttribute), true).FirstOrDefault();
                    if (webSocketActionAttribute != null)
                    {
                        if (!WebSocketActions.ContainsKey(webSocketActionAttribute.Action))
                        {
                            WebSocketActions[webSocketActionAttribute.Action] = new();
                        }
                        WebSocketActions[webSocketActionAttribute.Action].Add(type);
                    }
                }
            }
            if (Configuration.GetValue<bool>("SiteAnnouncement:Enabled"))
            {
                if (DateTimeOffset.TryParse(Configuration.GetValue<string>("SiteAnnouncement:ShowFrom"), out DateTimeOffset showFrom) && DateTimeOffset.TryParse(Configuration.GetValue<string>("SiteAnnouncement:ShowTo"), out DateTimeOffset showTo))
                {
                    string text = Configuration.GetValue<string>("SiteAnnouncement:Text") ?? string.Empty;
                    Announcement = new(showFrom, showTo, text);
                }
                else
                {
                    Log.LogWarning("Site announceement is enabled but no valid from/to date has been provided.");
                }
            }
            _ = InitEventListener();
        }

        private async Task InitEventListener()
        {
            try
            {
                ActiveMQ.Artemis.Client.Endpoint activeMqEndpont = ActiveMQ.Artemis.Client.Endpoint.Create(
                    Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                    Configuration.GetValue<int>("ActiveMQ:Port"),
                    Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                    Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);
                ConnectionFactory connectionFactory = new();
                await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont);

                List<(string queueName, RoutingType routingType)> queues = new();
                List<IEventListener> eventListeners = new();
                foreach (Type type in GetType().Assembly.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsClass && typeof(IEventListener).IsAssignableFrom(t)))
                {
                    if (ActivatorUtilities.CreateInstance(ServiceProvider, type) is not IEventListener eventListener)
                    {
                        continue;
                    }
                    eventListeners.Add(eventListener);
                    foreach ((string queueName, RoutingType routingType) in eventListener.Events)
                    {
                        if (!queues.Contains((queueName, routingType)))
                        {
                            queues.Add((queueName, routingType));
                        }
                    }
                }

                List<Task> tasks = new();
                foreach ((string queueName, RoutingType routingType) in queues)
                {
                    List<IEventListener> eventListener = eventListeners
                        .Where(e => e.Events.Contains((queueName, routingType)))
                        .ToList();
                    Task task = InitEventListenerEvent(connection, eventListener, queueName, routingType);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception in event listener initialisation");
            }
        }

        private async Task InitEventListenerEvent(IConnection connection, List<IEventListener> eventListeners, string queueName, RoutingType routingType)
        {
            await using IConsumer consumer = await connection.CreateConsumerAsync(queueName, routingType);
            while (true)
            {
                try
                {
                    Message message = await consumer.ReceiveAsync();
                    await using Transaction transaction = new();
                    await consumer.AcceptAsync(message, transaction);

                    await using AsyncServiceScope serviceScope = ServiceProvider.CreateAsyncScope();
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    string jsonString = message.GetBody<string>();
                    JObject json = JObject.Parse(jsonString);

                    foreach (IEventListener eventListener in eventListeners)
                    {
                        try
                        {
                            await eventListener.ProcessEvent(queueName, json, this, dbContext, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            Log.LogError(e, "Exception while processing event in {eventListener}", eventListener);
                        }
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Event listener exception");
                }
            }
        }

        private async ValueTask TriggerWebSocketAction(WebSocketSession webSocketSession, WebSocketAction webSocketAction, IServiceScopeFactory serviceScopeFactory, CancellationToken cancellationToken)
        {
            if (WebSocketActions.TryGetValue(webSocketAction, out List<Type>? webSocketActions))
            {
                await using AsyncServiceScope serviceScope = serviceScopeFactory.CreateAsyncScope();
                foreach (Type webSocketActionType in webSocketActions)
                {
                    try
                    {
                        IWebSocketAction action = (IWebSocketAction)ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, webSocketActionType);
                        await action.Process(webSocketSession, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e, "Error processing web socket action.");
                    }
                }
            }
        }

        public async Task ProcessRequest(HttpContext httpContext, ApplicationUser? applicationUser, IServiceScopeFactory serviceScopeFactory, CancellationToken cancellationToken)
        {
            using WebSocket ws = await httpContext.WebSockets.AcceptWebSocketAsync();
            if (ws.State != WebSocketState.Open)
            {
                return;
            }
            AuthenticationStatus authenticationStatus = new(applicationUser != null, Announcement);
            WebSocketMessage authenticationMessage = new("Authentication", authenticationStatus);
            await authenticationMessage.Send(ws, cancellationToken);
            WebSocketSession webSocketSession = new(ws, applicationUser);
            lock (WebSocketSessions)
            {
                WebSocketSessions.Add(webSocketSession);
            }
            await TriggerWebSocketAction(webSocketSession, WebSocketAction.OnUserConnected, serviceScopeFactory, cancellationToken);
            ArraySegment<byte> buffer = new(new byte[4096]);
            bool disconnect = false;
            while (ws.State == WebSocketState.Open && !disconnect)
            {
                await using MemoryStream message = new();

                WebSocketReceiveResult webSocketReceiveResult;
                try
                {
                    do
                    {
                        webSocketReceiveResult = await ws.ReceiveAsync(buffer, cancellationToken);
                        if (buffer.Array != null)
                        {
                            message.Write(buffer.Array, buffer.Offset, webSocketReceiveResult.Count);
                        }
                    }
                    while (!webSocketReceiveResult.EndOfMessage);
                }
                catch
                {
                    break;
                }
                message.Position = 0;
                switch (webSocketReceiveResult.MessageType)
                {
                    case WebSocketMessageType.Text:
                        {
                            await ProcessMessage(webSocketSession, message, serviceScopeFactory, cancellationToken);
                            break;
                        }
                    case WebSocketMessageType.Binary:
                        {
                            break;
                        }
                    case WebSocketMessageType.Close:
                        {
                            disconnect = true;
                            break;
                        }
                }
            }
            lock (WebSocketSessions)
            {
                WebSocketSessions.Remove(webSocketSession);
            }
            await TriggerWebSocketAction(webSocketSession, WebSocketAction.OnUserDisconnected, serviceScopeFactory, cancellationToken);
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                }
                catch { }
            }
        }

        public List<WebSocketSession> ActiveSessions
        {
            get
            {
                lock (WebSocketSessions)
                {
                    return new(WebSocketSessions);
                }
            }
        }

        private async Task ProcessMessage(WebSocketSession webSocketSession, MemoryStream messageStream, IServiceScopeFactory serviceScopeFactory, CancellationToken cancellationToken)
        {
            await using AsyncServiceScope serviceScope = serviceScopeFactory.CreateAsyncScope();
            string message = Encoding.UTF8.GetString(messageStream.ToArray());
            JObject messageObject = JObject.Parse(message);
            ICollection<ValidationError> validationErrors = WebSocketMessageReceivedSchema.Validate(messageObject);
            if (validationErrors.Count == 0)
            {
                WebSocketMessageReceived? webSocketMessage = messageObject.ToObject<WebSocketMessageReceived>();
                if (webSocketMessage?.Name != null && (WebSocketHandlers?.TryGetValue(webSocketMessage.Name, out Type? messageHandler) ?? false))
                {
                    EdDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<EdDbContext>();
                    ApplicationUser? user = null;
                    if (webSocketSession.UserId != null)
                    {
                        user = await dbContext.ApplicationUsers.SingleOrDefaultAsync(a => a.Id == webSocketSession.UserId, cancellationToken);
                    }
                    try
                    {
                        WebSocketHandler webSocketHandler = (WebSocketHandler)ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, messageHandler);
                        if (!webSocketHandler.AllowAnonymous && user == null)
                        {
                            WebSocketErrorMessage webSocketErrorMessage = new(webSocketMessage.Name, new List<string>() { "Unauthorized request" });
                            await webSocketErrorMessage.Send(webSocketSession.WebSocket, cancellationToken);
                        }
                        else if (webSocketHandler.ValidateMessageData(webSocketMessage.Data))
                        {
                            WebSocketHandlerResult result = await webSocketHandler.ProcessMessage(webSocketMessage, webSocketSession, user, dbContext, cancellationToken);
                            if (webSocketMessage.MessageId != null)
                            {
                                if (result is WebSocketHandlerResultSuccess webSocketHandlerResultSuccess)
                                {
                                    WebSocketResponseMessage responseMessage = new(webSocketMessage.Name, true, webSocketHandlerResultSuccess.ResponseData, webSocketMessage.MessageId);
                                    await responseMessage.Send(webSocketSession.WebSocket, cancellationToken);
                                    if (webSocketHandlerResultSuccess.ActiveObject != null)
                                    {
                                        webSocketSession.ActiveObject = webSocketHandlerResultSuccess.ActiveObject;
                                    }
                                }
                                else if (result is WebSocketHandlerResultError webSocketHandlerResultError)
                                {
                                    WebSocketErrorMessage webSocketErrorMessage = new(webSocketMessage.Name, webSocketHandlerResultError.Errors, webSocketMessage.MessageId);
                                    await webSocketErrorMessage.Send(webSocketSession.WebSocket, cancellationToken);
                                }
                            }
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                        else
                        {
                            WebSocketErrorMessage webSocketErrorMessage = new(webSocketMessage.Name, new List<string>() { "The message data received is not in the expected format." });
                            await webSocketErrorMessage.Send(webSocketSession.WebSocket, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(ex, "Error processing WebSocket Message");
                    }
                }
            }
        }

        class AuthenticationStatus
        {
            public bool IsAuthenticated { get; set; }
            public AuthenticationStatusAnnouncement? Announcement { get; set; }

            public AuthenticationStatus(bool isAuthenticated, AuthenticationStatusAnnouncement? announcement)
            {
                IsAuthenticated = isAuthenticated;
                Announcement = announcement;
            }
        }

        class AuthenticationStatusAnnouncement
        {
            public DateTimeOffset ShowFrom { get; set; }
            public DateTimeOffset ShowTo { get; set; }
            public string Text { get; }

            public AuthenticationStatusAnnouncement(DateTimeOffset showFrom, DateTimeOffset showTo, string text)
            {
                ShowFrom = showFrom;
                ShowTo = showTo;
                Text = text;
            }
        }
    }
}
