using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class StarSystemThargoidLevelChanged
    {
        public const string QueueName = "StarSystem.ThargoidLevelChanged";
        public const RoutingType Routing = RoutingType.Anycast;

        public long SystemAddress { get; set; }

        public StarSystemThargoidLevelChanged(long systemAddress)
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
                    DurabilityMode = DurabilityMode.Nondurable,
                    TimeToLive = TimeSpan.FromMinutes(1),
                };
            }
        }
    }
}
