using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text;

namespace EDOverwatch_Web.WebSockets
{
    public class WebSocketMessage
    {
        public string Name { get; set; }
        public object? Data { get; set; }
        public string? MessageId { get; set; }

        public WebSocketMessage(string name, object? data = null, string? messageId = null)
        {
            Name = name;
            Data = data;
            MessageId = messageId ?? Guid.NewGuid().ToString();
        }

        public ValueTask Send(WebSocketSession webSocketSession, CancellationToken cancellationToken)
            => Send(webSocketSession.WebSocket, cancellationToken);

        public ValueTask Send(WebSocket ws, CancellationToken cancellationToken)
        {
            if (ws.State == WebSocketState.Open)
            {
                string msg = JsonConvert.SerializeObject(this);
                ReadOnlyMemory<byte> message = new(Encoding.UTF8.GetBytes(msg));
                return ws.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
            }
            return ValueTask.CompletedTask;
        }
    }

    public class WebSocketResponseMessage : WebSocketMessage
    {
        public bool Success { get; set; }
        public DateTimeOffset Time => DateTimeOffset.UtcNow;
        public WebSocketResponseMessage(string name, bool success, object? data = null, string? messageId = null) :
            base(name, data, messageId)
        {
            Success = success;
        }
    }

    public class WebSocketErrorMessage : WebSocketResponseMessage
    {
        public List<string> Errors { get; }
        public WebSocketErrorMessage(string name, List<string> errors, string? messageId = null) :
            base(name, false, null, messageId)
        {
            Errors = errors;
        }
    }

    public class WebSocketMessageReceived
    {
        [Required]
        public string Name { get; set; }
        public JObject? Data { get; set; }
        public string? MessageId { get; set; }
        public string? CacheId { get; set; }

        public WebSocketMessageReceived(string name, JObject? data, string? messageId)
        {
            Name = name;
            Data = data;
            MessageId = messageId;
        }
    }
}
