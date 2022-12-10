using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Transactions;
using EDOverwatch_Web.sWebSocket.EventListener;
using Microsoft.Extensions.DependencyInjection;
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
        }

        private async Task InitEventListener()
        {
            ActiveMQ.Artemis.Client.Endpoint activeMqEndpont = ActiveMQ.Artemis.Client.Endpoint.Create(
                Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                Configuration.GetValue<int>("ActiveMQ:Port"),
                Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);
            ConnectionFactory connectionFactory = new();
            await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpont);

            List<Task> tasks = new();
            foreach (Type type in GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsClass && typeof(IEventListener).IsAssignableFrom(t)))
            {
                IEventListener? eventListener = Activator.CreateInstance(type) as IEventListener;
                if (eventListener == null)
                {
                    continue;
                }
                foreach (string eventName in eventListener.Events)
                {
                    Task task = InitEventListenerEvent(connection, eventListener, eventName);
                    tasks.Add(task);
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task InitEventListenerEvent(IConnection connection, IEventListener eventListener, string eventName)
        {
            await using IConsumer consumer = await connection.CreateConsumerAsync(eventName, RoutingType.Anycast);
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
                    await eventListener.ProcessEvent(json, this, dbContext);
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
            bool isAuthenticated = (applicationUser != null);
            WebSocketMessage authenticationMessage = new("Authentication", new AuthenticationStatus(isAuthenticated));
            await authenticationMessage.Send(ws, cancellationToken);
            if (!isAuthenticated || applicationUser == null)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return;
            }
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
                do
                {
                    webSocketReceiveResult = await ws.ReceiveAsync(buffer, cancellationToken);
                    if (buffer.Array != null)
                    {
                        message.Write(buffer.Array, buffer.Offset, webSocketReceiveResult.Count);
                    }
                }
                while (!webSocketReceiveResult.EndOfMessage);
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
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
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
                    ApplicationUser? user = await dbContext.ApplicationUsers.FindAsync(webSocketSession.User.Id, cancellationToken);
                    if (user != null)
                    {
                        try
                        {
                            WebSocketHandler webSocketHandler = (WebSocketHandler)ActivatorUtilities.CreateInstance(serviceScope.ServiceProvider, messageHandler);
                            if (webSocketHandler.ValidateMessageData(webSocketMessage.Data))
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
        }

        class AuthenticationStatus
        {
            public bool IsAuthenticated { get; set; }
            public AuthenticationStatus(bool isAuthenticated)
            {
                IsAuthenticated = isAuthenticated;
            }
        }
    }
}
