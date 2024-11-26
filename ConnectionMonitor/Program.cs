namespace ConnectionMonitor
{
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Monitor.Ingestion;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Xml;
    using System.Xml.Linq;
    using static ConnectionMonitor.Classes;

    class Program
    {
        public static List<Thread> threads = new List<Thread>();
        public static CancellationTokenSource tokenSource = new CancellationTokenSource();
        
        private static void Notify(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.FullPath} {e.ChangeType}");
        }

        static void Main(string[] args)
        {
            Thread watcher = new Thread(new ThreadStart(ConnectionFunctions.WatchConfig));
            watcher.Start();
        }
    }
}
