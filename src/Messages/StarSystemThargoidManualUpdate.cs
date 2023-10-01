using ActiveMQ.Artemis.Client;
using EDDatabase;
using Newtonsoft.Json;

namespace Messages
{
    public class StarSystemThargoidManualUpdate
    {
        public const string QueueName = "StarSystem.ThargoidManualUpdate";
        public const RoutingType Routing = RoutingType.Anycast;

        public long SystemAddress { get; set; }
        public string? SystemName { get; set; }
        public StarSystemThargoidLevelState State { get; set; }
        public short? Progress { get; set; }
        public short? DaysLeft { get; set; }

        public StarSystemThargoidManualUpdate(long systemAddress, string? systemName, StarSystemThargoidLevelState state, short? progress)
        {
            SystemAddress = systemAddress;
            SystemName = systemName;
            State = state;
            Progress = progress;
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
