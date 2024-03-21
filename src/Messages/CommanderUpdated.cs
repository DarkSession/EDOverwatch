using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class CommanderUpdated
    {
        public const string QueueName = "Commander.Updatedv2";
        public const RoutingType Routing = RoutingType.Multicast;

        public long FDevCustomerId { get; set; }
        public CommanderUpdated(long fDevCustomerId)
        {
            FDevCustomerId = fDevCustomerId;
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
