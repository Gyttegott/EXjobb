using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;

namespace ES_PS_analyzer
{
    public class LogAggregator
    {
        private BlockingCollection<JObject> IncomingQueue;
        private BlockingCollection<JObject> OutgoingQueue;
        private Tools.IRetentionCollection<JObject> RetentionQueue;
        private readonly object RetentionLock = new object();

        private Tools.IEntryContentWriter EntryWriter;
        private Tools.IErrorLogHandler ErrorLogger;

        private Func<JObject, string> TimeStampFinderFunction;

        public LogAggregator(BlockingCollection<JObject> IncomingQueue, BlockingCollection<JObject> OutgoingQueue, Tools.IRetentionCollection<JObject> RetentionQueue, Tools.IEntryContentWriter EntryWriter, Tools.IErrorLogHandler ErrorLogger, Func<JObject, string> TimeStampFinderFunction)
        {
            this.IncomingQueue = IncomingQueue;
            this.OutgoingQueue = OutgoingQueue;
            this.RetentionQueue = RetentionQueue;
            this.EntryWriter = EntryWriter;
            this.ErrorLogger = ErrorLogger;

            this.TimeStampFinderFunction = TimeStampFinderFunction;
        }

        public void FetchLog()
        {
            JObject log = IncomingQueue.Take();
            try
            {
                lock (RetentionLock)
                {
                    RetentionQueue.InsertElement(log);
                }
            }
            catch (FormatException)
            {
                Debug.WriteLine("Log entry has missing or corrupt timestamp field");
                string id = Guid.NewGuid().ToString();
                EntryWriter.WriteContent(id, Encoding.ASCII.GetBytes(log.ToString()));
                ErrorLogger.LogError(string.Format("Log entry had missing or corrupt timestamp. {0}", EntryWriter.GetStorageDescription(id)));

            }
        }
        public void SendAggregationsOfAge(double MinAgeSeconds)
        {
            List<JObject> Results;
            lock (RetentionLock)
            {
                Results = RetentionQueue.ExtractElementsOlderThan(MinAgeSeconds);
            }

            var res = Results.OrderBy(x => DateTime.Parse(TimeStampFinderFunction(x)));
            foreach(var log in res)
            {
                OutgoingQueue.Add(log);
            }
        }

    }

}
