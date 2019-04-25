using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    class DateTimeProvider : ITimeProvider
    {
        public DateTime Now()
        {
            return DateTime.Now;
        }

        public DateTime Parse(string DateTimeString)
        {
            return DateTime.Parse(DateTimeString);
        }
    }
}
