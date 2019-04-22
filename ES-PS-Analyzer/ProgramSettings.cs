using System.Collections.Generic;
using System.Configuration;

namespace ES_PS_analyzer
{
    /// <summary>
    /// Used for reading in application settings 
    /// </summary>
    class ProgramSettings
    {
        //Hashtable for settings
        private Dictionary<string, string> dict;

        public ProgramSettings()
        {
            dict = new Dictionary<string, string>();
            //read in all settings and insert them into the hashtable
            foreach (var k in ConfigurationManager.AppSettings.AllKeys)
            {
                dict.Add(k, ConfigurationManager.AppSettings[k]);
            }
        }

        /// <summary>
        /// Returns the setting for a given key
        /// </summary>
        /// <param name="key">The key of the setting</param>
        /// <returns>The value of the setting</returns>
        public string getSetting(string key)
        {
            return dict[key];
        }
    }

}
