using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer
{
    public interface INetworkHandler
    {
        void Connect(string Address, int Port);

        void Disconnect();

        void SendData(byte[] data);

        byte[] RetrieveData();
    }
}
