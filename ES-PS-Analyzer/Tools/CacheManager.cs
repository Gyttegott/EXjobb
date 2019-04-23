using System.Collections.Generic;
using System.Threading.Tasks;

namespace ES_PS_analyzer.Tools
{
    class CacheManager : ICommandCache
    {
        private Dictionary<string, PSInfo> CommandCache = new Dictionary<string, PSInfo>();
        private object CommandCacheLock = new object();

        public async Task<PSInfo> GetLastCommand(string Host)
        {
            PSInfo res;
            lock (CommandCacheLock)
            {
                try
                {
                    res = CommandCache[Host];
                }
                catch
                {
                    res = null;
                }
            }

            return res;
        }

        public void SetLastCommand(string Host, PSInfo LastCommand)
        {
            lock (CommandCacheLock)
            {
                CommandCache[Host] = LastCommand;
            }
        }
    }
}