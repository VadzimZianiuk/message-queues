using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Rabbit
{
    public class MessageQueue : IReaderMQ, IWriterMQ, IDisposable
    {
        private const string DefaultHostName = "localhost";
        private const string DefaultQueueName = "TestQueue";
        private const uint DefaultMaxMessageSize = 1024 * 1024;

        private readonly ILogger<MessageQueue> _logger;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;
        private bool disposed;

        public event EventHandler<BasicDeliverEventArgs> Received
        {
            add
            {
                if (value != null)
                {
                    _consumer.Received += value;
                }
            }
            remove => _consumer.Received -= value;
        }

        public MessageQueue()
            : this(DefaultHostName, DefaultMaxMessageSize, DefaultQueueName) { }

        public MessageQueue(string hostName, uint maxMessageSize, string queueName, ILogger<MessageQueue> logger)
        {
            _factory = new ConnectionFactory()
            {
                HostName = hostName,
                MaxMessageSize = maxMessageSize
            };

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _consumer = new EventingBasicConsumer(_channel);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.BasicConsume(queueName, autoAck: true, consumer: _consumer);
        }
        public void Publish(string routingKey, byte[] body)
        {
            
        }

        

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _connection.Dispose();
                    _channel.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}