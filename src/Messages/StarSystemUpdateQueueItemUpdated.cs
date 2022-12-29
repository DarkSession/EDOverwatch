using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class StarSystemUpdateQueueItemUpdated
    {
        public const string QueueName = "StarSystem.UpdateQueueItemUpdated";
        public const RoutingType Routing = RoutingType.Multicast;

        public int Id { get; set; }

        public StarSystemUpdateQueueItemUpdated(int id)
        {
            Id = id;
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
