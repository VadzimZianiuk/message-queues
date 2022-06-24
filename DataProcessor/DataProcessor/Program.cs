using CommandLine;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;

            using var connection = CreateConnection(options);
            using var channel = CreateChannel(connection, options);

            Console.WriteLine("Data processor was started! Press enter to exit...");
            Console.ReadLine();
        }

        private static IConnection CreateConnection(Options options)
        {
            var factory = new ConnectionFactory()
            {
                HostName = options.Host,
                Port = options.Port
            };

            return factory.CreateConnection();
        }

        private static IModel CreateChannel(IConnection connection, Options options)
        {
            var channel = connection.CreateModel();
            channel.QueueDeclare(options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (object s, BasicDeliverEventArgs e) =>
            {
                var fileName = e.BasicProperties.MessageId;
                var part = (int)e.BasicProperties.Headers["part"];
                var batchSize = (int)e.BasicProperties.Headers["batch-size"];

                using var fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
                fs.Write(e.Body.Span);
                fs.Flush();
                fs.Close();

                Console.WriteLine($"{e.BasicProperties.MessageId} [{e.Body.Length} bytes] {part}/{batchSize} was recieved");
                
            };
            channel.BasicConsume(options.QueueName, autoAck: true, consumer);

            return channel;
        }
    }

}