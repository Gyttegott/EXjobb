using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    public interface IFileWriter
    {
        void WriteFile(string FilePath, byte[] Content);
    }
}
