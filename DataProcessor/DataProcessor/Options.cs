using CommandLine;

namespace DataProcessor
{
    internal class Options
    {
        [Option("dataPath", Required = false)]
        public string Path { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        [Option('h', "host", Required = false, Default = "localhost")]
        public string Host { get; set; }

        [Option('p', "port", Required = false, Default = 5672)]
        public int Port { get; set; }

        [Option('q', "queueName", Required = false, Default = "TestQueue")]
        public string QueueName { get; set; }
    }
}
