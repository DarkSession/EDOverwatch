using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class StationUpdated
    {
        public const string QueueName = "Station.Updated";
        public const RoutingType Routing = RoutingType.Anycast;
        public long MarketId { get; set; }
        public long SystemAddress { get; set; }

        public StationUpdated(long marketId, long systemAddress)
        {
            MarketId = marketId;
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
                    TimeToLive = TimeSpan.FromDays(1),
                };
            }
        }
    }
}
