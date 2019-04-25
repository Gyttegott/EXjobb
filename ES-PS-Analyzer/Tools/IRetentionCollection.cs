using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    public interface IRetentionCollection<T>
    {
        void InsertElement(T Element);

        List<T> ExtractElementsOlderThan(double Seconds);
    }
}
