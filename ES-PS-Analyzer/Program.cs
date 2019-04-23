﻿using System;
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

        /// <summary>
        /// Default constructor parsing a JSON object
        /// </summary>
        /// <param name="Json">JSON object</param>
        public PSInfo (JObject Json)
        {
            //powershell_main_command = (string)Json["powershell"]["main_command"];
            powershell_script_name = (string)Json["powershell"]["script_name"];
            powershell_command = (string)Json["powershell"]["command"];
            powershell_host_application = (string)Json["powershell"]["host_application"];

            //handle missing parameters field
            var tmp = Json["powershell"]["parameters"];
            powershell_parameters = tmp == null ? new string[] { } : tmp.ToObject<string[]>();
            powershell_risk = 0;
            timestamp = Json["@timestamp"].ToObject<DateTime>();
            computer_name = (string)Json["winlog"]["computer_name"];
        }

    }

    /// <summary>
    /// Global data that is to be accesible for multiple parts of the program
    /// </summary>
    class ProgramData
    {
        //The pool of all incoming logs that has yet to be processed
        public static List<IncomingLog> IncomingPool = new List<IncomingLog>();
        //Lock object for asynchronos access to the incoming log pool
        public static object IncomingPoolLock = new object();
        //public static EventWaitHandle IncomingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        //The pool of all outgoing logs that has yet to be sent
        public static List<string> OutgoingPool = new List<string>();
        //Lock object for asynchronos access to the outgoing log pool
        public static object OutgoingPoolLock = new object();
        //public static EventWaitHandle OutgoingHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        //Global risk table for looking up configurations of risks
        public static RiskLookup RiskLookupTable = new RiskLookup();
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

            //Start async functions for processing logs and sending processed logs
            //Task.Run(() => LogProcessingStarter());
            //Task.Run(() => LogSender("localhost", 9555));

            
            return;


        }

        /// <summary>
        /// Async function that polls the incoming pool for logs and starts new async function to process them
        /// </summary>
        static void LogProcessingStarter()
        {
            //Create application settings object
            var Configuration = new ProgramSettings();

            //Create the risk calculator
            var StartHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursStart"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var EndHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursEnd"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var RiskCalculator = new RiskCalculator(StartHour.Hour + (StartHour.Minute / 60), EndHour.Hour + (EndHour.Minute / 60));

            //Create the ElasticSearch query client
            var ESClient = new ELasticsearchQuerier(Configuration.getSetting("ConnectionUrl"));

            //Create the cache handler
            var Cache = new CacheManager();

            //Start the log polling
            while (true)
            {
                //Lock the incoming log pool and extract all logs of an configured age. This is done since incoming logs may come unordered chronologically
                List<IncomingLog> Results;
                lock (ProgramData.IncomingPoolLock)
                {
                    Results = ProgramData.IncomingPool.FindAll(x => (DateTime.Now - x.obtained).TotalMinutes > 0.1);
                    foreach(var res in Results)
                    {
                        ProgramData.IncomingPool.Remove(res);
                    }
                }
                //If logs of enough high age is found, group them by the host they were run on and start individual async processes for each group
                if (Results.Count() > 0)
                {
                    var groups = Results.GroupBy(x => (string)x.log["winlog"]["computer_name"]);
                    foreach(var g in groups)
                    {
                        Task.Run(() => ProcessLog(g, RiskCalculator, Cache, ESClient));
                    }
                }
                else
                {
                    Debug.WriteLine("[DEBUG] No new logs to process, trying again in 10 sec");
                }
                //Sleep for a configured time to wait for more logs of certain age
                System.Threading.Thread.Sleep(10000);
            }

        }

        /// <summary>
        /// Async function used for processing logs of a certain host
        /// </summary>
        /// <param name="logs">The collection of logs to process</param>
        /// <param name="Calculator">The calculator to use when determening command risks</param>
        /// <param name="Cache">The cache manager to use for caching values</param>
        /// <param name="ESClient">The ElasticSearch client to use when querying</param>
        static void ProcessLog(IEnumerable<IncomingLog> logs, RiskCalculator Calculator, CacheManager Cache, ELasticsearchQuerier ESClient)
        {

            string Host = (string)logs.First().log["winlog"]["computer_name"];
            //Get the last run command if cached by previous processing
            PSInfo LastCommand = Cache.GetLastCommand(Host);

            //Sort all the logs by execution timestamp ascending
            var SortedLogs = logs.Select(x => x.log).OrderBy(x => DateTime.Parse((string)x["@timestamp"]));

            //If the last run command was not in cache, query ElasticSearch for it
            /*if(LastCommand == null)
            {
                var ESCall = await ESClient.GetLastCommand(Host);
                LastCommand = ESCall == null ? null : ESCall.Source;
            }*/

            //Process all the logs in chronological order
            foreach(JObject log in SortedLogs)
            {

                var CurrentCommand = new PSInfo(log);

                //Calculate the risk level for the command
                var RiskLevel = Calculator.GetRisk(CurrentCommand, LastCommand);
                log["powershell"]["risk"] = RiskLevel;
                CurrentCommand.powershell_risk = RiskLevel;

                //Lock the outgoing log pool and insert the log with its new risk value
                lock (ProgramData.OutgoingPoolLock)
                {
                    ProgramData.OutgoingPool.Add(log.ToString(Formatting.None));
                }
                LastCommand = CurrentCommand;
            }

            //Insert the last processed command for future processing for this host
            Cache.SetLastCommand(LastCommand.computer_name, LastCommand);
        }

    }

}
