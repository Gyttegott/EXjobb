using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Network
{
    class TcpClientHandler : INetworkHandler
    {
        private TcpClient Client;

        public TcpClientHandler()
        {
            Client = new TcpClient();
        }

        ~TcpClientHandler()
        {
            Client.Close();
        }

        public void Connect(string Address, int Port)
        {
            Client.Connect(Address, Port);
        }

        public void Disconnect()
        {
            Client.Close();
        }

        public byte[] RetrieveData()
        {
            throw new NotImplementedException();
        }

        public void SendData(byte[] data)
        {
            Client.GetStream().Write(data, 0, data.Length);
        }
    }
}
