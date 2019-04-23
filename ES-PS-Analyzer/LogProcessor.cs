using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

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

    class LogProcessor
    {
        private BlockingCollection<JObject> IncomingQueue;
        private BlockingCollection<JObject> OutgoingQueue;

        private List<IncomingLog> RetentionQueue;
        private readonly object RetentionLock = new object();

        private Tools.ICommandCache FirstLevelCache;
        private Tools.ICommandCache SecondLevelCache;
        private RiskEvaluation.IRiskCalculator Calculator;
        private Tools.ITimeProvider TimeService;

        public LogProcessor(BlockingCollection<JObject> InputStore, BlockingCollection<JObject> OutputStore, Tools.ICommandCache FirstLevelCache, RiskEvaluation.IRiskCalculator RiskEvaluater, Tools.ITimeProvider TimeProvider, Tools.ICommandCache SecondLevelCache = null)
        {
            IncomingQueue = InputStore;
            OutgoingQueue = OutputStore;
            this.FirstLevelCache = FirstLevelCache;
            Calculator = RiskEvaluater;
            this.SecondLevelCache = SecondLevelCache;
            TimeService = TimeProvider;

            RetentionQueue = new List<IncomingLog>();
        }

        public void TimeStampIncomingLog()
        {
            RetentionQueue.Add(new IncomingLog {
                log = IncomingQueue.Take(),
                obtained = TimeService.Now()
            });
        }

        public List<IncomingLog> GetRetentionQueue()
        {
            return RetentionQueue;
        }

        public IEnumerable<IGrouping<string, IncomingLog>> GetGroupedAndExpiredLogs(int MinAgeSeconds)
        {
            List<IncomingLog> Results;
            lock (RetentionLock)
            {
                Results = RetentionQueue.FindAll(x => (TimeService.Now() - x.obtained).TotalSeconds > MinAgeSeconds);
                foreach (var res in Results)
                {
                    RetentionQueue.Remove(res);
                }
            }

            return Results.GroupBy(x => (string)x.log["winlog"]["computer_name"]);
        }

        public void ProcessLog(IEnumerable<IncomingLog> logs)
        {

            string Host = (string)logs.First().log["winlog"]["computer_name"];
            //Get the last run command if cached by previous processing
            PSInfo LastCommand = FirstLevelCache.GetLastCommand(Host).GetAwaiter().GetResult();

            //Sort all the logs by execution timestamp ascending
            var SortedLogs = logs.Select(x => x.log).OrderBy(x => DateTime.Parse((string)x["@timestamp"]));

            //If the last run command was not in cache, query ElasticSearch for it
            if(LastCommand == null && SecondLevelCache != null)
            {
                LastCommand = SecondLevelCache.GetLastCommand(Host).GetAwaiter().GetResult();
            }

            //Process all the logs in chronological order
            foreach(JObject log in SortedLogs)
            {

                var CurrentCommand = new PSInfo(log);

                //Calculate the risk level for the command
                var RiskLevel = Calculator.CalculateRisk(CurrentCommand, LastCommand);
                log["powershell"]["risk"] = RiskLevel;
                CurrentCommand.powershell_risk = RiskLevel;

                //Send the processed log to output queue
                OutgoingQueue.Add(log);

                LastCommand = CurrentCommand;
            }

            //Insert the last processed command for future processing for this host
            FirstLevelCache.SetLastCommand(LastCommand.computer_name, LastCommand);
        }
    }

}
