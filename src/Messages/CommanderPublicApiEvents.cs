using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Messages
{
    public class CommanderPublicApiEvents
    {
        public const string QueueName = "Commander.PublicApiEvents";
        public const RoutingType Routing = RoutingType.Anycast;

        public int CommanderId { get; set; }
        public List<JObject>? Events { get; set; }

        public CommanderPublicApiEvents(int commanderId)
        {
            CommanderId = commanderId;
        }

        [JsonIgnore]
        public Message Message
        {
            get
            {
                string body = JsonConvert.SerializeObject(this);
                return new(body);
            }
        }
    }
}
