using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace ES_PS_analyzer.Network
{
    public class JSONNetworkSender
    {
        private BlockingCollection<JObject> SendingQueue;
        private INetworkHandler SendingClient;
        private Tools.IErrorLogHandler ErrorLogger;
        private string DestinationAddress;
        private int DestinationPort;
        private bool Started;

        public JSONNetworkSender(BlockingCollection<JObject> queue, INetworkHandler sender, Tools.IErrorLogHandler error, string address, int port)
        {
            SendingQueue = queue;
            DestinationAddress = address;
            DestinationPort = port;
            Started = false;
            SendingClient = sender;
            ErrorLogger = error;
        }

        public void SendLog()
        {

            JObject log = SendingQueue.Take();

            //Send log
            byte[] sarr = Encoding.ASCII.GetBytes(log.ToString(Newtonsoft.Json.Formatting.None) + '\n');
            try
            {
                SendingClient.SendData(sarr);
            }
            catch (Exception)
            {
                try
                {
                    SendingClient.Connect(DestinationAddress, DestinationPort);
                    SendingClient.SendData(sarr);
                }
                catch (Exception)
                {
                    ErrorLogger.LogError(string.Format("Could not establish a connection to {0}:{1}", DestinationAddress, DestinationPort));
                    SendingQueue.Add(log);
                }
            }
        }
    }

}
