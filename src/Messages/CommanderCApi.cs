using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class CommanderCApi
    {
        public const string QueueName = "Commander.CApi";
        public const RoutingType Routing = RoutingType.Anycast;

        public long FDevCustomerId { get; set; }
        public CommanderCApi(long fDevCustomerId)
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
