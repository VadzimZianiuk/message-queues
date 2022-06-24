namespace Rabbit
{
    public interface IWriterMQ : IDisposable
    {
        public void Publish(string routingKey, byte[] body);
    }
}