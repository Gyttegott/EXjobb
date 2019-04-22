using System.Collections.Generic;

namespace ES_PS_analyzer
{
    class CacheManager
    {
        private Dictionary<string, PSInfo> CommandCache = new Dictionary<string, PSInfo>();
        private object CommandCacheLock = new object();
        public PSInfo GetLastCommand(string host)
        {
            PSInfo res;
            lock (CommandCacheLock)
            {
                try
                {
                    res = CommandCache[host];
                }
                catch
                {
                    res = null;
                }
            }

            return res;
        }

        public void SetLastCommand(string host, PSInfo command)
        {
            lock (CommandCacheLock)
            {
                CommandCache[host] = command;
            }
        }
    }
}