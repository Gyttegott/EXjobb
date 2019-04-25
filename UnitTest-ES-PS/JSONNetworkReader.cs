using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Moq;
using System.Text;
using System.Linq;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class JSONNetworkReader
    {

        [TestMethod]
        public void Parses_One_Message()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            string incString = "{\"hej\":2}";
            byte[] incBarr = Encoding.ASCII.GetBytes(incString);
            JObject parsedLog;

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            mockNetwork.Setup(t => t.RetrieveData()).Returns(incBarr);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockFileReader = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkReader(coll, mockNetwork.Object, mockFileReader.Object, mockErrorLogger.Object, "10.10.10.12", 4500);

            //Act
            obj.ReadData();

            //Assert
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "10.10.10.12"), It.Is<int>(x => x == 4500)));
            mockNetwork.Verify(t => t.RetrieveData());
            mockNetwork.Verify(t => t.Disconnect());
            mockNetwork.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockFileReader.VerifyNoOtherCalls();

            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual(incString, parsedLog.ToString(Newtonsoft.Json.Formatting.None));
        }


        [TestMethod]
        public void Parses_Multiple_Messages_In_Sequential_Order()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            string incString = "{\"hej\":2}\n{\"hej\":3}\n{\"hej\":4}";
            byte[] incBarr = Encoding.ASCII.GetBytes(incString);
            JObject parsedLog;

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            mockNetwork.Setup(t => t.RetrieveData()).Returns(incBarr);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockFileReader = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkReader(coll, mockNetwork.Object, mockFileReader.Object, mockErrorLogger.Object, "10.10.10.12", 4500);

            //Act
            obj.ReadData();

            //Assert
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "10.10.10.12"), It.Is<int>(x => x == 4500)));
            mockNetwork.Verify(t => t.RetrieveData());
            mockNetwork.Verify(t => t.Disconnect());
            mockNetwork.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockFileReader.VerifyNoOtherCalls();

            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":2}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":3}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":4}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
        }

        [TestMethod]
        public void Parses_Multiple_Messages_In_Sequential_Order_With_Ending_Newline()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            string incString = "{\"hej\":2}\n{\"hej\":3}\n{\"hej\":4}\n";
            byte[] incBarr = Encoding.ASCII.GetBytes(incString);
            JObject parsedLog;

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            mockNetwork.Setup(t => t.RetrieveData()).Returns(incBarr);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockFileReader = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkReader(coll, mockNetwork.Object, mockFileReader.Object, mockErrorLogger.Object, "10.10.10.12", 4500);

            //Act
            obj.ReadData();

            //Assert
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "10.10.10.12"), It.Is<int>(x => x == 4500)));
            mockNetwork.Verify(t => t.RetrieveData());
            mockNetwork.Verify(t => t.Disconnect());
            mockNetwork.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockFileReader.VerifyNoOtherCalls();

            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":2}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":3}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":4}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
        }

        [TestMethod]
        public void Parse_Error_On_One_Leads_To_Error_Logging_And_No_Passthrough()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            string incString = "{\"hej\"2}\n";
            byte[] incBarr = Encoding.ASCII.GetBytes(incString);
            JObject parsedLog;

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            mockNetwork.Setup(t => t.RetrieveData()).Returns(incBarr);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockFileReader = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkReader(coll, mockNetwork.Object, mockFileReader.Object, mockErrorLogger.Object, "10.10.10.12", 4500);

            //Act
            obj.ReadData();

            //Assert
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "10.10.10.12"), It.Is<int>(x => x == 4500)));
            mockNetwork.Verify(t => t.RetrieveData());
            mockNetwork.Verify(t => t.Disconnect());
            mockNetwork.VerifyNoOtherCalls();

            mockErrorLogger.Verify(t => t.LogError(It.IsAny<string>()));
            mockErrorLogger.VerifyNoOtherCalls();

            mockFileReader.Verify(t => t.WriteContent(It.IsAny<string>(), It.Is<byte[]>(x => x.SequenceEqual(incBarr))));
            mockFileReader.Verify(t => t.GetStorageDescription(It.IsAny<string>()));
            mockFileReader.VerifyNoOtherCalls();

            Assert.IsTrue(!coll.TryTake(out parsedLog));
        }

        [TestMethod]
        public void Parse_Error_On_Subset_Leads_To_Error_Logging_And_Passthrough()
        {
            //Arrange
            var coll = new BlockingCollection<JObject>();
            string incString = "{\"hej\":2\n{\"hej\":3}\n{\"hej\":4}\n"; ;
            byte[] incBarr = Encoding.ASCII.GetBytes(incString);
            JObject parsedLog;

            Mock<ES_PS_analyzer.Network.INetworkHandler> mockNetwork = new Mock<ES_PS_analyzer.Network.INetworkHandler>();
            mockNetwork.Setup(t => t.RetrieveData()).Returns(incBarr);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockFileReader = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.Network.JSONNetworkReader(coll, mockNetwork.Object, mockFileReader.Object, mockErrorLogger.Object, "10.10.10.12", 4500);

            //Act
            obj.ReadData();

            //Assert
            mockNetwork.Verify(t => t.Connect(It.Is<string>(x => x == "10.10.10.12"), It.Is<int>(x => x == 4500)));
            mockNetwork.Verify(t => t.RetrieveData());
            mockNetwork.Verify(t => t.Disconnect());
            mockNetwork.VerifyNoOtherCalls();

            mockErrorLogger.Verify(t => t.LogError(It.IsAny<string>()));
            mockErrorLogger.VerifyNoOtherCalls();

            mockFileReader.Verify(t => t.WriteContent(It.IsAny<string>(), It.Is<byte[]>(x => Encoding.ASCII.GetString(x) == "{\"hej\":2\n{\"hej\":3}")));
            mockFileReader.Verify(t => t.GetStorageDescription(It.IsAny<string>()));
            mockFileReader.VerifyNoOtherCalls();

            Assert.IsTrue(coll.TryTake(out parsedLog));
            Assert.AreEqual("{\"hej\":4}", parsedLog.ToString(Newtonsoft.Json.Formatting.None));
        }
    }
}
