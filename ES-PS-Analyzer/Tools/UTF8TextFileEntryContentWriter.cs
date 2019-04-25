using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    class UTF8TextFileEntryContentWriter : IEntryContentWriter
    {
        private string StoragePath;

        public UTF8TextFileEntryContentWriter(string StorageDirectory)
        {
            StoragePath = StorageDirectory;
            Directory.CreateDirectory(StorageDirectory);
        }


        public string GetStorageDescription(string Identifier)
        {
            return string.Format("The content can be found at {0}\\{1}.txt", StoragePath, Identifier);
        }

        public byte[] ReadContent(string Identifier)
        {
            return Encoding.UTF8.GetBytes(ReadContentAsString(Identifier));
        }

        public string ReadContentAsString(string Identifier)
        {
            string FilePath = string.Format("{0}\\{1}.txt", StoragePath, Identifier);
            string retval = File.ReadAllText(FilePath, Encoding.UTF8);
            return retval;
        }

        public void WriteContent(string Identifier, byte[] Content)
        {
            WriteContent(Identifier, Encoding.UTF8.GetString(Content));
        }

        public void WriteContent(string Identifier, string Content)
        {
            string FilePath = string.Format("{0}\\{1}.txt", StoragePath, Identifier);
            File.WriteAllText(FilePath, Content);
        }
    }
}
