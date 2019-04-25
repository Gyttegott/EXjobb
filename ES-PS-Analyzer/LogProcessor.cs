using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Collections.Generic;

namespace ES_PS_analyzer
{
    /// <summary>
    /// Used for grouping incoming logs with the datetime that they were received
    /// </summary>
    class IncomingLog
    {
        //The log in JSON format
        public JObject log { get; set; }

        //The time it was received in this program
        public DateTime obtained { get; set; }
    }

    public class LogProcessor
    {
        private BlockingCollection<JObject> IncomingQueue;
        private BlockingCollection<JObject> OutgoingQueue;

        private Tools.ICommandCache FirstLevelCache;
        private Tools.ICommandCache SecondLevelCache;
        private RiskEvaluation.IRiskCalculator Calculator;
        private Tools.IEntryContentWriter EntryWriter;
        private Tools.IErrorLogHandler ErrorLogger;

        private Func<JObject, PSInfo> InfoParser;
        private Action<JObject, double> RiskAdder;

        public LogProcessor(BlockingCollection<JObject> InputStore, BlockingCollection<JObject> OutputStore, Tools.ICommandCache FirstLevelCache, RiskEvaluation.IRiskCalculator RiskEvaluater, Tools.IEntryContentWriter writer, Tools.IErrorLogHandler error, Func<JObject, PSInfo> InfoParser, Action<JObject, double> RiskAdder, Tools.ICommandCache SecondLevelCache = null)
        {
            IncomingQueue = InputStore;
            OutgoingQueue = OutputStore;
            this.FirstLevelCache = FirstLevelCache;
            Calculator = RiskEvaluater;
            this.SecondLevelCache = SecondLevelCache;

            EntryWriter = writer;
            ErrorLogger = error;

            this.InfoParser = InfoParser;
            this.RiskAdder = RiskAdder;
        }

        public void ProcessLog()
        {
            var log = IncomingQueue.Take();

            PSInfo LogInfo = InfoParser(log);

            if (string.IsNullOrWhiteSpace(LogInfo.computer_name))
            {
                Debug.WriteLine("Log entry has missing host name");
                string id = Guid.NewGuid().ToString();
                EntryWriter.WriteContent(id, Encoding.ASCII.GetBytes(log.ToString()));
                ErrorLogger.LogError(string.Format("Log entry had missing host name. {0}", EntryWriter.GetStorageDescription(id)));
                return;
            }

            //Get the last run command if cached by previous processing
            PSInfo LastCommand = FirstLevelCache.GetLastCommand(LogInfo.computer_name).GetAwaiter().GetResult();

            //If the last run command was not in cache, query ElasticSearch for it
            if(LastCommand == null && SecondLevelCache != null)
            {
                LastCommand = SecondLevelCache.GetLastCommand(LogInfo.computer_name).GetAwaiter().GetResult();
            }

            //Calculate the risk level for the command
            var RiskLevel = Calculator.CalculateRisk(LogInfo, LastCommand);
            RiskAdder(log, RiskLevel);
            LogInfo.powershell_risk = RiskLevel;

            //Send the processed log to output queue
            OutgoingQueue.Add(log);

            //Insert the last processed command for future processing for this host
            FirstLevelCache.SetLastCommand(LogInfo.computer_name, LogInfo);
        }
    }

}
