using CommandLine;

namespace DataCapture
{
    internal class Options
    {
        [Option("dataPath", Required = false)]
        public string DataPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        [Option("dataFilter", Required = false, Default = "*.pdf")]
        public string DataFilter { get; set; }

        [Option('h', "host", Required = false, Default = "localhost")]
        public string Host { get; set; }

        [Option('p', "port", Required = false, Default = 5672)]
        public int Port { get; set; }

        [Option('q', "queueName", Required = false, Default = "TestQueue")]
        public string QueueName { get; set; }

        [Option("maxMessageSize", Required = false, Default = 1024 * 1024)]
        public int MaxMessageSize { get; set; }
    }
}
