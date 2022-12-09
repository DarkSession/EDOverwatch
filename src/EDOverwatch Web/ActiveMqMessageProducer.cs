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

        public Task SendAsync(string address, RoutingType routingType, object body, CancellationToken cancellationToken = default)
        {
            Message msg = new(body);
            return Producer.SendAsync(address, routingType, msg, cancellationToken);
        }
    }
}
