using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Runtime.Caching;
using System.Diagnostics;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ES_PS_analyzer
{
    class PSDoc
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_source")]
        public PSInfo Source {get;set;}
    }

    class PSInfo
    {
        public string powershell_main_command { get; set; }

        public string powershell_script_name { get; set; }

        public string powershell_command { get; set; }

        public string powershell_host_application { get; set; }

        public List<string> powershell_parameters { get; set; }

        [JsonProperty("@timestamp")]
        public DateTime timestamp { get; set; }

        public double powershell_risk { get; set; }

        public string computer_name { get; set; }

    }

    class IncomingLog
    {
        public JObject log { get; set; }

        public DateTime obtained { get; set; }
    }

    class ProgramData
    {
        public static List<IncomingLog> IncomingPool = new List<IncomingLog>();
        public static object IncomingPoolLock = new object();
        //public static EventWaitHandle IncomingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public static List<string> OutgoingPool = new List<string>();
        public static object OutgoingPoolLock = new object();
        //public static EventWaitHandle OutgoingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public static RiskLookup RiskLookupTable = new RiskLookup();
    }

    class CacheManager
    {
        private MemoryCache LastCommandCache = new MemoryCache("PowerShellCommands");
        private object LastCommandCacheLock = new object();

        public PSInfo GetLastCommand(string HostName)
        {
            object res;
            lock (LastCommandCacheLock)
            {
                res = LastCommandCache[HostName];
            }
            return (PSInfo)res;
        }

        public void SetLastCommand(string HostName, PSInfo Command)
        {
            lock (LastCommandCacheLock)
            {
                LastCommandCache[HostName] = Command;
            }
        }
    }

    class Program
    {


        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Task.Run(() => LogProcessingStarter());
            Task.Run(() => LogSender("localhost", 9555));

            TcpListener Server = null;
            try
            {
                // TcpListener server = new TcpListener(port);
                Server = new TcpListener(IPAddress.Any, 9432);

                // Start listening for client requests.
                Server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = Server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            ms.Write(bytes, 0, i);

                            // Process the data sent by the client.
                            if (!stream.DataAvailable)
                            {
                                var data = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                                ms.SetLength(0);
                                var split = Regex.Split(data, "(?<=\\})\\n(?=\\{)");

                                lock (ProgramData.IncomingPoolLock)
                                {
                                    foreach (string log in split)
                                    {
                                        var ParsedLog = JObject.Parse(log);
                                        if (!ProgramData.RiskLookupTable.CommandExist((string)ParsedLog["powershell_command"]))
                                        {
                                            using (var writer = new StreamWriter("C:\\Users\\jmn\\inetpuben\\ES-PS-analyzer\\ES-PS-analyzer\\UnseenCommands.txt", true))
                                            {
                                                var pars = (JArray)ParsedLog["powershell_parameters"];
                                                writer.WriteLine((string)ParsedLog["powershell_command"] + " " + (pars == null ? "" : string.Join(" ", pars)));
                                            }
                                            continue;
                                        }

                                        ProgramData.IncomingPool.Add(new IncomingLog{
                                            log = ParsedLog,
                                            obtained = DateTime.Now
                                        });
                                    }
                                }
                            }

                            /*byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                            // Send back a response.
                            stream.Write(msg, 0, msg.Length);
                            Console.WriteLine("Sent: {0}", data);*/
                        }
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                Server.Stop();
            }
            return;


        }

        static void LogProcessingStarter()
        {
            var Configuration = new ProgramSettings();

            var StartHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursStart"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var EndHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursEnd"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var RiskCalculator = new RiskCalculator(StartHour.Hour + (StartHour.Minute / 60), EndHour.Hour + (EndHour.Minute / 60));

            // Set up a connection object to ElasticSearch
            var ESClient = new ELasticsearchQuerier(Configuration.getSetting("ConnectionUrl"), Configuration.getSetting("PreprocessIndex"));

            var Cache = new CacheManager();

            while (true)
            {
                List<IncomingLog> Results;
                lock (ProgramData.IncomingPoolLock)
                {
                    Results = ProgramData.IncomingPool.FindAll(x => (DateTime.Now - x.obtained).TotalMinutes > 0.1);
                    foreach(var res in Results)
                    {
                        ProgramData.IncomingPool.Remove(res);
                    }
                }
                if (Results.Count() > 0)
                {
                    var groups = Results.GroupBy(x => (string)x.log["computer_name"]);
                    foreach(var g in groups)
                    {
                        Task.Run(() => ProcessLog(g, RiskCalculator, Cache, ESClient));
                    }
                }
                else
                {
                    Debug.WriteLine("[DEBUG] No new logs to process, trying again in 10 sec");
                }
                System.Threading.Thread.Sleep(10000);
            }

        }

        static async void ProcessLog(IEnumerable<IncomingLog> logs, RiskCalculator Calculator, CacheManager Cache, ELasticsearchQuerier ESClient)
        {
            string Host = (string)logs.First().log["computer_name"];
            var SortedLogs = logs.Select(x => x.log).OrderBy(x => DateTime.Parse((string)x["@timestamp"]));
            PSInfo LastCommand = Cache.GetLastCommand(Host);

            if(LastCommand == null)
            {
                var ESCall = await ESClient.GetLastCommand(Host);
                LastCommand = ESCall == null ? null : ESCall.Source;
            }

            foreach(JObject log in SortedLogs)
            {
                var CurrentCommand = log.ToObject<PSInfo>();

                //Calculate the risk level for the command
                var RiskLevel = Calculator.GetRisk(CurrentCommand, LastCommand);
                log["powershell_risk"] = RiskLevel;
                CurrentCommand.powershell_risk = RiskLevel;

                lock (ProgramData.OutgoingPoolLock)
                {
                    ProgramData.OutgoingPool.Add(log.ToString(Formatting.None));
                }
                LastCommand = CurrentCommand;
            }

            Cache.SetLastCommand(LastCommand.computer_name, LastCommand);
        }

        static void LogSender(string Server, int Port)
        {
            TcpClient Client = new TcpClient();

            while (true)
            {
                List<string> SendString = null;

                lock (ProgramData.OutgoingPoolLock)
                {
                    if(ProgramData.OutgoingPool.Count() > 0)
                    {
                        SendString = new List<string>(ProgramData.OutgoingPool);
                        ProgramData.OutgoingPool.Clear();
                    }
                }

                if(SendString != null)
                {
                    if (!Client.Connected)
                        Client.Connect(Server, Port);

                    StreamWriter Writer = new StreamWriter(Client.GetStream(), Encoding.ASCII);
                    foreach(var str in SendString)
                    {
                        byte[] sarr = Encoding.ASCII.GetBytes(str+'\n');
                        Client.GetStream().Write(sarr, 0, sarr.Length);
                    }
                }

                System.Threading.Thread.Sleep(10000);
            }

        }

    }

}
