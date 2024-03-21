using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class ThargoidMaelstromCreatedUpdated
    {
        public const string QueueName = "ThargoidMaelstrom.CreatedUpdatedV2";
        public const RoutingType Routing = RoutingType.Multicast;
        public int Id { get; set; }
        public string Name { get; set; }

        public ThargoidMaelstromCreatedUpdated(int id, string name)
        {
            Id = id;
            Name = name;
        }

        [JsonIgnore]
        public Message Message
        {
            get
            {
                string body = JsonConvert.SerializeObject(this);
                return new(body)
                {
                    DurabilityMode = DurabilityMode.Nondurable,
                    TimeToLive = TimeSpan.FromMinutes(1),
                };
            }
        }
    }
}
