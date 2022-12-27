using ActiveMQ.Artemis.Client;

namespace EDOverwatch_Web
{
    public class ActiveMqMessageProducer
    {
        private IAnonymousProducer Producer { get; }
        public ActiveMqMessageProducer(IAnonymousProducer producer)
        {
            Producer = producer;
        }

        public Task SendAsync(string address, RoutingType routingType, Message msg, CancellationToken cancellationToken = default)
        {
            return Producer.SendAsync(address, routingType, msg, cancellationToken);
        }
    }
}
