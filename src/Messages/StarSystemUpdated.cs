using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class StarSystemUpdated
    {
        public const string QueueName = "StarSystem.Updated";
        public const RoutingType Routing = RoutingType.Multicast;

        public long SystemAddress { get; set; }

        public StarSystemUpdated(long systemAddress)
        {
            SystemAddress = systemAddress;
        }

        [JsonIgnore]
        public Message Message
        {
            get
            {
                string body = JsonConvert.SerializeObject(this);
                return new(body)
                {
                    DurabilityMode = DurabilityMode.Durable,
                };
            }
        }
    }
}