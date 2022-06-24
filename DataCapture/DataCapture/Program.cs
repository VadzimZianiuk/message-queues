using CommandLine;
using RabbitMQ.Client;
using System.Text;

namespace DataCapture
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;

            using var cts = new CancellationTokenSource();
            using var connection = CreateConnection(options);
            using var channel = CreateChannel(connection, options.QueueName);
            using var watcher = StartWatcher(channel, options, cts.Token);
            Console.WriteLine("Start capture data in: {0}", options.DataPath);
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
            cts.Cancel();
        }

        private static IConnection CreateConnection(Options options)
        {
            var factory = new ConnectionFactory()
            {
                HostName = options.Host,
                Port = options.Port,
                MaxMessageSize = (uint)options.MaxMessageSize
            };

            return factory.CreateConnection();
        }

        private static IModel CreateChannel(IConnection connection, string queueName)
        {
            var channel = connection.CreateModel();
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            return channel;
        }

        private static FileSystemWatcher StartWatcher(IModel channel, Options options, CancellationToken token)
        {
            var watcher = new FileSystemWatcher(options.DataPath, options.DataFilter)
            {
                EnableRaisingEvents = true,
            };
            
            var files = new DirectoryInfo(options.DataPath).GetFiles(options.DataFilter, SearchOption.TopDirectoryOnly);

            watcher.Created += async (object s, FileSystemEventArgs e) =>
            {
                if (!File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
                {
                    try
                    {
                        await PublishAsync(channel, options, e.FullPath, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            };

            files.AsParallel().ForAll(async fi => await PublishAsync(channel, options, fi.FullName, token).ConfigureAwait(false));

            return watcher;
        }

        private static async Task<byte[]> GetBytesAsync(string filePath, CancellationToken token)
        {
            const int retryTimeout = 1000;

            while (true)
            {
                await Task.Delay(retryTimeout, token).ConfigureAwait(false);
                try
                {
                    return await File.ReadAllBytesAsync(filePath, token).ConfigureAwait(false);
                }
                catch (IOException) { }
            }
        }

        private static async Task PublishAsync(IModel channel, Options options, string filePath, CancellationToken token)
        {
            var bytes = await GetBytesAsync(filePath, token);
            string fileName = Path.GetFileName(filePath);

            Console.WriteLine("Start sending a file: {0} [{1} bytes]", fileName, bytes.Length);
            
            if (bytes.Length <= options.MaxMessageSize)
            {
                var props = GetProperties(channel, fileName);
                channel.BasicPublish(string.Empty, options.QueueName, props, bytes);
            }
            else
            {
                var sb = new StringBuilder();
                var batch = channel.CreateBasicPublishBatch();

                var batchSize = (int)Math.Ceiling((double)bytes.Length / options.MaxMessageSize);
                var startIndex = 0;
                var length = options.MaxMessageSize;
                for (int i = 1; i <= batchSize; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var data = new ReadOnlyMemory<byte>(bytes, startIndex, length);
                    var props = GetProperties(channel, fileName, i, batchSize);
                    batch.Add(string.Empty, options.QueueName, true, props, data);
                    
                    sb.AppendLine($"Preparing {i}/{batchSize} {fileName} [{length} bytes]");
                    startIndex += length;
                    length = Math.Min(length, bytes.Length - startIndex);
                }

                batch.Publish();
                --sb.Length;
                Console.WriteLine(sb);
            }

            File.Delete(fileName);
            Console.WriteLine("File {0} [{1} bytes] has been sent.", fileName, bytes.Length);
        }

        private static IBasicProperties GetProperties(IModel channel, string fileName) => GetProperties(channel, fileName, 1, 1);

        private static IBasicProperties GetProperties(IModel channel, string fileName, int part, int batchSize)
        {
            var props = channel.CreateBasicProperties();
            props.MessageId = fileName;
            props.Headers = new Dictionary<string, object>()
            {
                { "batch-size", batchSize },
                { "part", part}
            };
            return props;
        }
    }
}