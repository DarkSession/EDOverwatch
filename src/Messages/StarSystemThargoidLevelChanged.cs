using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class StarSystemThargoidLevelChanged
    {
        public const string QueueName = "StarSystem.ThargoidLevelChanged";
        public const RoutingType Routing = RoutingType.Multicast;

        public long SystemAddress { get; set; }
        public bool Changed { get; set; }

        public StarSystemThargoidLevelChanged(long systemAddress, bool changed)
        {
            SystemAddress = systemAddress;
            Changed = changed;
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
