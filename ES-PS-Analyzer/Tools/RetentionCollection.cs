using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    public class RetentionCollection<T> : IRetentionCollection<T>
    {
        private class TimeStampedElement<Y>
        {
            public Y Element { get; set; }
            public DateTime TimeStamp { get; set; }
        }

        private List<TimeStampedElement<T>> ElementList;
        private Tools.ITimeProvider TimeProvider;
        private Func<T, DateTime> ElementTimeStamper;

        public RetentionCollection(Tools.ITimeProvider TimeProvider, Func<T, string> TimeStampFieldFinder)
        {
            this.TimeProvider = TimeProvider;
            ElementList = new List<TimeStampedElement<T>>();
            ElementTimeStamper = x => this.TimeProvider.Parse(TimeStampFieldFinder(x));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Element"></param>
        /// <exception cref="FormatException"></exception>
        public void InsertElement(T Element)
        {
            DateTime tmp;
            try
            {
                tmp = ElementTimeStamper(Element);
            }
            catch
            {
                throw new FormatException("Field is not a valid DateTime string");
            }

            ElementList.Add(new TimeStampedElement<T>
            {
                Element = Element,
                TimeStamp = tmp
            });
        }

        public List<T> ExtractElementsOlderThan(double Seconds)
        {
            var SearchResult = ElementList.Where(x => (TimeProvider.Now() - x.TimeStamp).TotalSeconds > Seconds).ToList();
            var test = ElementList.Select(x => (TimeProvider.Now() - x.TimeStamp).TotalSeconds).ToList();
            foreach (var res in SearchResult)
            {
                ElementList.Remove(res);
            }

            return SearchResult.Select(x => x.Element).ToList();
        }
    }
}
