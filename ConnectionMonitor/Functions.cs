using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Ingestion;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static ConnectionMonitor.Classes;

namespace ConnectionMonitor
{
    public static class Classes
    {
        public static string lastWrite = "";

        public class NmapResult
        {
            public Nmaprun nmaprun { get; set; }
            public string result { get; set; } = "";

            public class Address
            {
                public string addr { get; set; }
                public string addrtype { get; set; }
            }

            public class Host
            {
                public Status status { get; set; }
                public Address address { get; set; }
                public object hostnames { get; set; }
                public Ports ports { get; set; }
            }

            public class Nmaprun
            {
                public Scaninfo scaninfo { get; set; }
                public Host host { get; set; }
            }

            public class Port
            {
                public string protocol { get; set; }
                public string portid { get; set; }
                public State state { get; set; }
            }

            public class Ports
            {
                public Port port { get; set; }
            }

            public class Scaninfo
            {
                public string type { get; set; }
                public string protocol { get; set; }
                public string numservices { get; set; }
                public string services { get; set; }
            }

            public class State
            {
                public string state { get; set; }
                public string reason { get; set; }
                public string reason_ttl { get; set; }
            }

            public class Status
            {
                public string state { get; set; }
                public string reason { get; set; }
                public string reason_ttl { get; set; }
            }
        }
        public class PingResult
        {
            public int packets_transmitted { get; set; }
            public string destination { get; set; } = "";
            public int packets_received { get; set; }
            public float packet_loss_percent { get; set; }
            public string status { get; set; } = "";
        }
        public class ConnectionResult
        {
            public string Time { get; set; } = "";
            public string Computer { get; set; } = "";
            public string Status { get; set; } = "unknown";
            public string Host { get; set; } = "unknown";
            public string Protocol { get; set; } = "unknown";
            public int Port { get; set; }
            //public string State { get; set; }
            //public string Reason { get; set; }
            //public string ReasonTtl { get; set; }
            public dynamic AdditionalContext { get; set; } = "";
            public dynamic RawContext { get; set; } = "";
        }

        public class ConnectionDefinition
        {
            public string Computer { get; set; } = "";
            public string Port { get; set; } = "";
            public string Name { get; set; } = "";
            public string Protocol { get; set; } = "";
            public int Interval { get; set; } = 0;
        }
    }

    public class ConnectionFunctions
    {
        public static async Task PostResult(ConnectionResult connectionResult)
        {
            var ruleId = Environment.GetEnvironmentVariable("dataCollectionRuleImmutableId");
            var endpointString = Environment.GetEnvironmentVariable("dataCollectionRulelogsIngestion");

            if (string.IsNullOrEmpty(endpointString))
            {
                throw new ArgumentNullException(nameof(endpointString), "The endpoint URL cannot be null or empty.");
            }

            var endpoint = new Uri(endpointString);
            var streamName = "Custom-ConnMon_CL";

            var credential = new DefaultAzureCredential();
            LogsIngestionClient client = new(endpoint, credential);

            List<ConnectionResult> connectionResults = new List<ConnectionResult>();
            connectionResults.Add(connectionResult);

            BinaryData data = BinaryData.FromObjectAsJson(connectionResults);

            try
            {
                var response = await client.UploadAsync(ruleId, streamName, RequestContent.Create(data));
                if (response.IsError)
                {
                    throw new Exception(response.ToString());
                }

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Log upload completed using content upload");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Upload failed with Exception: " + ex.Message);
            }
        }

        public static void WatchConfig()
        {
            List<ConnectionDefinition>? connectionTests = new List<ConnectionDefinition>();
            while (true)
            {
                //Console.WriteLine($"Running Threads {Program.threads.Count}");


                string connectionTestsFile = "./config/connectionTests.json";
                if (File.Exists(connectionTestsFile))
                {
                    if (lastWrite != File.GetLastWriteTimeUtc(connectionTestsFile).ToString("yyyy-MM-dd HH:mm:ss"))
                    {
                        lastWrite = File.GetLastWriteTimeUtc(connectionTestsFile).ToString("yyyy-MM-dd HH:mm:ss");
                        Console.WriteLine($"LastWrite {lastWrite}");
                        JsonSerializerOptions options = new JsonSerializerOptions
                        {
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };


                        try
                        {
                            connectionTests = JsonSerializer.Deserialize<List<ConnectionDefinition>>(File.ReadAllText(connectionTestsFile), options);
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error parsing JSON: {ex.Message}");
                            continue;
                        }

                        if (null != connectionTests)
                        {
                            foreach (ConnectionDefinition connectionDefinition in connectionTests)
                            {
                                string connectionTestName = $"{connectionDefinition.Computer}_{connectionDefinition.Port}_{connectionDefinition.Protocol}_{connectionDefinition.Interval}";
                                if (!Program.threads.Any(t => t.Name == connectionTestName))
                                {
                                    Console.WriteLine($"Starting thread for {connectionTestName}");
                                    Thread connectionThread = new Thread(new ParameterizedThreadStart(TestConnection));
                                    connectionThread.Name = connectionTestName;
                                    connectionThread.Start(connectionDefinition);
                                    Program.threads.Add(connectionThread);
                                }
                            }

                            // Stop threads that dont exist in the connectionTests

                            for (int i = 0; i < Program.threads.Count; i++)
                            {
                                Thread thread = Program.threads[i];
                                if (!connectionTests.Any(c => thread.Name == $"{c.Computer}_{c.Port}_{c.Protocol}_{c.Interval}"))
                                {
                                    Console.WriteLine($"Stopping thread {thread.Name}");
                                    thread.Name = $"_stop_{thread.Name}";
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"File {connectionTestsFile} does not exist");
                }

                Thread.Sleep(1000);
            }
        }

        public static void TestConnection(object testParametersObject)
        {
            ConnectionDefinition testParameters = (ConnectionDefinition)Convert.ChangeType(testParametersObject, typeof(ConnectionDefinition));

            int interval = testParameters.Interval;
            string computer = testParameters.Computer;
            string port = testParameters.Port;
            string protocol = testParameters.Protocol;
            bool active = true;
            while (active)
            {
                string currentTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                ConnectionResult connectionResult;

                string command = "";


                if (protocol.ToLower() != "icmp")
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Testing connection to {computer} on port {port} with protocol {protocol}");
                    string parameters = "";
                    if (protocol.Length > 0)
                    {
                        if (protocol.ToLower() == "tcp")
                        {
                            parameters = "-v -R";
                        }
                        else
                        {
                            parameters = "-v -R -sU";
                        }
                    }
                    command = $"/usr/bin/nmap {parameters} -p{port} -oX - {computer} | jc --xml -p";

                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Testing connection to {computer} with protocol {protocol}");
                    string parameters = "";
                    command = $"/usr/bin/ping {parameters} -c 4 {computer} | jc --ping -p";
                }

                Process process = new Process
                {
                    StartInfo =
                        {
                            FileName = "bash",
                            ArgumentList = { "-c", "--", command },
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false
                        }
                };
                process.Start();
                try
                {
                    process.WaitForExit();
                }
                catch (ThreadInterruptedException ex)
                {

                }

                string output = process.StandardOutput.ReadToEnd().Replace("@", "");
                string errorOutput = process.StandardError.ReadToEnd().Replace("@", "");

                dynamic? dynamicOutput = JsonSerializer.Deserialize<dynamic>(output);

                PingResult pingResult = new PingResult();
                NmapResult nmapResult = new NmapResult();

                if (protocol.ToLower() == "icmp")
                {
                    pingResult = JsonSerializer.Deserialize<PingResult>(output);
                    if (Convert.ToInt16(pingResult.packet_loss_percent) == 0)
                    {
                        pingResult.status = "up";
                    }
                    else
                    {
                        pingResult.status = "down";
                    }
                }
                else
                {
                    nmapResult = JsonSerializer.Deserialize<NmapResult>(output);
                    if (nmapResult.nmaprun.host.ports.port.state.state.Contains("filtered"))
                    {
                        nmapResult.result = "down";
                    }
                    else
                    {
                        nmapResult.result = "up";
                    }
                }

                connectionResult = new ConnectionResult
                {
                    Time = currentTime,
                    Computer = computer,
                    Status = protocol.ToLower() == "icmp" ? pingResult.status : nmapResult.result,
                    Host = protocol.ToLower() == "icmp" ? pingResult.destination : nmapResult.nmaprun.host.address.addr,
                    Protocol = protocol.ToLower(),
                    Port = protocol.ToLower() == "icmp" ? 0 : Convert.ToInt16(port),
                    AdditionalContext = dynamicOutput ?? new { }
                };

                PostResult(connectionResult).ConfigureAwait(false);

                Thread.Sleep(interval * 1000);
                if (Thread.CurrentThread.Name.Substring(0, 6) == "_stop_")
                {
                    active = false;
                    Program.threads.Remove(Thread.CurrentThread);
                }
            }
        }
    }
}
