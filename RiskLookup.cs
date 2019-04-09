using System;
using System.Collections;
using System.IO;

namespace ES_PS_analyzer
{
    class RiskLookup
    {
        private Hashtable ht;

        public RiskLookup()
        {
            try
            {
                ht = new Hashtable();
                foreach (string line in File.ReadAllLines("CommandRiskMappings.txt"))
                {
                    var splits = line.Split(' ');
                    if (splits.Length == 1 || splits.Length > 2)
                        throw new FileLoadException("Wrong format");
                    ht[splits[0]] = int.Parse(splits[1]);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public int getRisk(string command)
        {
            try
            {
                return (int)ht[command];
            }
            catch
            {
                return 0;
            }
        }
    }
}
