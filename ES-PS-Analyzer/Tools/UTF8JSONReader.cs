using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    class UTF8JSONReader : IEntryContentWriter
    {
        public string GetStorageDescription(string FilePath)
        {
            return "";
        }

        public byte[] ReadContent(string FilePath)
        {
            return Encoding.UTF8.GetBytes(ReadContentAsString(FilePath));
        }

        public string ReadContentAsString(string FilePath)
        {
            return File.ReadAllText(FilePath);
        }

        public void WriteContent(string FilePath, byte[] Content)
        {
            throw new NotImplementedException();
        }

        public void WriteContent(string FilePath, string Content)
        {
            throw new NotImplementedException();
        }
    }
}
