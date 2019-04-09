using System.Collections.Generic;
using System.Configuration;

namespace ES_PS_analyzer
{
    class ProgramSettings
    {
        private Dictionary<string, string> dict;
        public ProgramSettings()
        {
            dict = new Dictionary<string, string>();
            foreach (var k in ConfigurationManager.AppSettings.AllKeys)
            {
                dict.Add(k, ConfigurationManager.AppSettings[k]);
            }
        }

        public string getSetting(string key)
        {
            return dict[key];
        }
    }

}
