using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace ES_PS_analyzer
{
    public class JSONNetworkSender
    {
        private BlockingCollection<JObject> SendingQueue;
        private INetworkHandler SendingClient;
        private string DestinationAddress;
        private int DestinationPort;
        private bool Started;

        public JSONNetworkSender(BlockingCollection<JObject> queue, INetworkHandler sender, string address, int port)
        {
            SendingQueue = queue;
            DestinationAddress = address;
            DestinationPort = port;
            Started = false;
            SendingClient = sender;
        }

        public void StartSending()
        {
            if (Started)
            {
                return;
            }

            Started = true;
            SendingClient.Connect(DestinationAddress, DestinationPort);

            //Start sending loop
            while (true)
            {
                JObject log = SendingQueue.Take();

                //Send log
                byte[] sarr = Encoding.ASCII.GetBytes(log.ToString(Newtonsoft.Json.Formatting.None) + '\n');
                try
                {
                    SendingClient.SendData(sarr);
                }
                catch (ObjectDisposedException)
                {
                    SendingClient.Connect(DestinationAddress, DestinationPort);
                    SendingClient.SendData(sarr);
                }
            }
        }
    }

}
