using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    class UTF8TextFileErrorLogger : IErrorLogHandler
    {
        private string LogFilePath;

        public UTF8TextFileErrorLogger(string FilePath)
        {
            LogFilePath = FilePath;

            var directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);
        }

        public void LogError(string Error)
        {
            using(var sw = File.AppendText(LogFilePath))
            {
                sw.WriteLine(Error);
            }
        }
    }
}
