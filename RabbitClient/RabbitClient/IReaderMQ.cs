using RabbitMQ.Client.Events;

namespace Rabbit
{
    public interface IReaderMQ : IDisposable
    {
        public event EventHandler<BasicDeliverEventArgs> Received;
    }
}
