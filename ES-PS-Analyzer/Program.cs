using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;

namespace ES_PS_analyzer
{

    /// <summary>
    ///  Used for holding commands and their associated contexts
    /// </summary>
    public class PSInfo
    {
        //The full command line issued
        //public string powershell_main_command { get; set; }

        //The path and script name of the script the current command is executed from
        public string powershell_script_name { get; set; }

        //The current executing command
        public string powershell_command { get; set; }

        //The executable that called for the command
        public string powershell_host_application { get; set; }

        //All parameters given to the command
        public string[] powershell_parameters { get; set; }

        //When the command was run in ISO8601 UTC format
        public DateTime timestamp { get; set; }

        //The risk calculated for the given command
        public double powershell_risk { get; set; }

        //The full computer name that the command was run on
        public string computer_name { get; set; }

    }

    /// <summary>
    /// Main program
    /// </summary>
    class Program
    {


        /// <summary>
        /// Main function
        /// </summary>
        /// <param name="args">Command line argument, currently not used</param>
        static void Main(string[] args)
        {
            //Set debug writing to console stream
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            //Create Channels used
            var AggregationQueue = new BlockingCollection<JObject>();
            var ProcessingQueue = new BlockingCollection<JObject>();
            var OutgoingQueue = new BlockingCollection<JObject>();

            //Creat error logger and log writer
            var ErrorLogger = new Tools.UTF8TextFileErrorLogger("Logs\\ErrorLog.txt");
            var LogEntryWriter = new Tools.UTF8TextFileEntryContentWriter("Logs\\Entries\\");

            //Create TCP reader
            var TcpServerWrapper = new Network.TcpServerHandler(IPAddress.Any, 9432, 2048);
            var NetworkReader = new Network.JSONNetworkReader(AggregationQueue, TcpServerWrapper, LogEntryWriter, ErrorLogger, "0.0.0.0", 9432);

            //Create Aggregator
            Func<JObject, string> TimeStampFinder = x => (string)x["@timestamp"];
            var DateTimeWrapper = new Tools.DateTimeProvider();
            var AggregationCollection = new Tools.RetentionCollection<JObject>(DateTimeWrapper, TimeStampFinder);
            var Aggregator = new LogAggregator(AggregationQueue, ProcessingQueue, AggregationCollection, LogEntryWriter, ErrorLogger, TimeStampFinder);

            //Create Processor
            var MemoryCache = new Tools.CacheManager();
            var JSONReader = new Tools.UTF8JSONReader();
            var RiskLookup = new RiskEvaluation.RiskLookup(JSONReader);
            var RiskEvaluator = new RiskEvaluation.RiskCalculator(RiskLookup, 7.5, 16.5);
            Func<JObject, PSInfo> LogParser = x => {
                var tmp = x["powershell"]["parameters"];
                return new PSInfo
                {
                    powershell_script_name = (string)x["powershell"]["script_name"],
                    powershell_command = (string)x["powershell"]["command"],
                    powershell_host_application = (string)x["powershell"]["host_application"],
                    powershell_parameters = tmp == null ? new string[] { } : tmp.ToObject<string[]>(),
                    powershell_risk = 0,
                    timestamp = x["@timestamp"].ToObject<DateTime>(),
                    computer_name = (string)x["winlog"]["computer_name"]
                };
            };
            Action<JObject, double> RiskStamper = (x, y) => x["powershell"]["risk"] = y;
            var Processor = new LogProcessor(ProcessingQueue, OutgoingQueue, MemoryCache, RiskEvaluator, LogEntryWriter, ErrorLogger, LogParser, RiskStamper);

            //Create TCP sender
            var TcpClientWrapper = new Network.TcpClientHandler();
            var NetworkSender = new Network.JSONNetworkSender(OutgoingQueue, TcpClientWrapper, ErrorLogger, "localhost", 9555);




            //Fire up all async processes

            //Reading incoming network
            Task.Run(() =>
            {
                while (true)
                    NetworkReader.ReadData();
            });

            //Aggregating logs
            Task.Run(() =>
            {
                while (true)
                    Aggregator.FetchLog();
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Aggregator.SendAggregationsOfAge(6);
                    Thread.Sleep(6000);
                }
            });

            //Processing logs
            Task.Run(() =>
            {
                while (true)
                    Processor.ProcessLog();
            });

            //Reading incoming network
            while (true)
                NetworkSender.SendLog();



            return;


        }

        

    }

}
