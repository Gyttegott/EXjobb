using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ES_PS_analyzer;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class TcpLogSender_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            var coll = new BlockingCollection<JObject>();
            var obj = new ES_PS_analyzer.JSONNetworkSender()
        }
    }
}
