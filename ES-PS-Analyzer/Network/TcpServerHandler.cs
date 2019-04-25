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
    class TcpServerHandler : INetworkHandler
    {
        private TcpListener Server;
        private TcpClient ConnectedPeer;
        private byte[] ReceiveBuffer;

        public TcpServerHandler(IPAddress ListeningAddress, int ListeningPort, int BufferSize)
        {
            ReceiveBuffer = new byte[BufferSize];
            Server = new TcpListener(ListeningAddress, ListeningPort);
            Server.Start();
        }

        ~TcpServerHandler()
        {
            Server.Stop();
        }

        public void Connect(string Address, int Port)
        {
            if(ConnectedPeer == null || !ConnectedPeer.Connected)
                ConnectedPeer = Server.AcceptTcpClient();
        }

        public void Disconnect()
        {
            ConnectedPeer.Close();
        }

        public byte[] RetrieveData()
        {
            var stream = ConnectedPeer.GetStream();
            using(var ms = new MemoryStream())
            {
                do
                {
                    int i = stream.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
                    ms.Write(ReceiveBuffer, 0, i);
                } while (stream.DataAvailable);

                var retval = ms.ToArray();
                ms.SetLength(0);

                return retval;
            }
        }

        public void SendData(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
