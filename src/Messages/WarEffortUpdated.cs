﻿using ActiveMQ.Artemis.Client;
using Newtonsoft.Json;

namespace Messages
{
    public class WarEffortUpdated
    {
        public const string QueueName = "WarEffort.UpdatedV2";
        public const RoutingType Routing = RoutingType.Multicast;

        public long SystemAddress { get; }
        public long? FDevCustomerId { get; }

        public WarEffortUpdated(long systemAddress, long? fDevCustomerId)
        {
            SystemAddress = systemAddress;
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
