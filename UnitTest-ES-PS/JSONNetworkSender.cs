using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ES_PS_analyzer;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Linq;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class TcpLogSender_Test
    {
        [TestMethod]
        public void Waits_For_Data_Before_Connecting()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "localhost", 6000);

            //Act
            var task = Task.Run(() => obj.SendLog());
            Thread.Sleep(500);

            //Assert
            mockNetwork.VerifyNoOtherCalls();

            //Clean up
            coll.Add(new JObject());
        }

        [TestMethod]
        public void Open_Connection_Leads_To_Only_Sending()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            var sendObject = new JObject();
            coll.Add(sendObject);

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "localhost", 6000);

            //Act
            obj.SendLog();


            //Assert
            mockNetwork.Verify(t => t.SendData(It.IsAny<byte[]>()));
            mockNetwork.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Closed_Connection_Leads_To_Reconnect()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            var sendObject = new JObject();
            coll.Add(sendObject);

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            mockNetwork.SetupSequence(f => f.SendData(It.IsAny<byte[]>()))
                .Throws(new ObjectDisposedException(""))
                .Pass();

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "localhost", 6000);

            //Act
            obj.SendLog();


            //Assert
            mockNetwork.Verify(t => t.SendData(It.IsAny<byte[]>()));
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "localhost"), It.Is<int>(x => x == 6000)));
            mockNetwork.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Sent_Data_Is_Formatted_As_Oneliners()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            var sendObject = JObject.Parse(@"
{
    ""hej"": 2
}");
            var bytes = Encoding.ASCII.GetBytes("{\"hej\":2}\n");
            coll.Add(sendObject);

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            mockNetwork.SetupSequence(f => f.SendData(It.IsAny<byte[]>()))
                .Throws(new ObjectDisposedException(""))
                .Pass();

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "localhost", 6000);

            //Act
            obj.SendLog();


            //Assert
            mockNetwork.Verify(t => t.SendData(It.Is<byte[]>(x => x.SequenceEqual(bytes))));
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "localhost"), It.Is<int>(x => x == 6000)));
            mockNetwork.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Unable_To_Connect_Leads_To_Reinsertion()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            var sendObject = new JObject();
            JObject reinsertedObj;
            coll.Add(sendObject);

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            mockNetwork.SetupSequence(f => f.SendData(It.IsAny<byte[]>()))
                .Throws(new ObjectDisposedException(""))
                .Throws(new ObjectDisposedException(""));

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "localhost", 6000);

            //Act
            obj.SendLog();


            //Assert
            mockNetwork.Verify(t => t.SendData(It.IsAny<byte[]>()));
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "localhost"), It.Is<int>(x => x == 6000)));
            mockNetwork.VerifyNoOtherCalls();

            Assert.IsTrue(coll.TryTake(out reinsertedObj));
            Assert.AreEqual(sendObject, reinsertedObj);

        }

        [TestMethod]
        public void Unable_To_Connect_Leads_To_Error_Logging()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            var sendObject = new JObject();
            coll.Add(sendObject);

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            mockNetwork.SetupSequence(f => f.SendData(It.IsAny<byte[]>()))
                .Throws(new ObjectDisposedException(""))
                .Throws(new ObjectDisposedException(""));

            var obj = new ES_PS_analyzer.Network.JSONNetworkSender(coll, mockNetwork.Object, mockErrorLogger.Object, "10.10.10.10", 6000);

            //Act
            obj.SendLog();


            //Assert
            mockErrorLogger.Verify(t => t.LogError(It.Is<string>(x => x.Contains("10.10.10.10") && x.Contains("6000"))));

        }
    }
}
