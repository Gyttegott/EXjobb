using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    public interface IEntryContentWriter
    {
        void WriteContent(string Identifier, byte[] Content);

        void WriteContent(string Identifier, string Content);

        byte[] ReadContent(string Identifier);

        string ReadContentAsString(string Identifier);

        string GetStorageDescription(string Identifier);
    }
}
