using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Moq;
using System.Collections.Generic;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class LogAggregator
    {
        [TestMethod]
        public void Fetch_Inserts_Same_Object_Into_Retention_Collection()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            InputColl.Add(jobj1);

            Func<JObject, string> TimeStampFinder = x => (string)x["time"];

            Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>> mockRetQ = new Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>>();
            mockRetQ.Setup(t => t.InsertElement(It.Is<JObject>(x => x == jobj1))).Verifiable();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogAggregator(InputColl, OutputColl, mockRetQ.Object, mockEntryWriter.Object, mockErrorLogger.Object, TimeStampFinder);

            //Act
            obj.FetchLog();


            //Assert
            mockRetQ.Verify();
            mockRetQ.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Inserts_With_Exception_Yields_Error_And_Drop_Event()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            InputColl.Add(jobj1);

            Func<JObject, string> TimeStampFinder = x => (string)x["time"];

            Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>> mockRetQ = new Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>>();
            mockRetQ.Setup(t => t.InsertElement(It.Is<JObject>(x => x == jobj1))).Throws(new FormatException()).Verifiable();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogAggregator(InputColl, OutputColl, mockRetQ.Object, mockEntryWriter.Object, mockErrorLogger.Object, TimeStampFinder);

            //Act
            obj.FetchLog();


            //Assert
            mockRetQ.Verify();
            mockRetQ.VerifyNoOtherCalls();

            mockErrorLogger.Verify(t => t.LogError(It.IsAny<string>()));
            mockEntryWriter.Verify(t => t.WriteContent(It.IsAny<string>(), It.IsAny<byte[]>()));
            mockEntryWriter.Verify(t => t.GetStorageDescription(It.IsAny<string>()));
            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Send_Calls_With_Same_Age_On_Retention_Collection()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");

            List<JObject> testList = new List<JObject>();

            Func<JObject, string> TimeStampFinder = x => (string)x["time"];

            Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>> mockRetQ = new Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>>();
            mockRetQ.Setup(t => t.ExtractElementsOlderThan(It.Is<double>(x => x == 4))).Returns(testList).Verifiable();
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogAggregator(InputColl, OutputColl, mockRetQ.Object, mockEntryWriter.Object, mockErrorLogger.Object, TimeStampFinder);

            //Act
            obj.SendAggregationsOfAge(4);


            //Assert
            mockRetQ.Verify();
            mockRetQ.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Nothing_To_send_Sends_Nothing()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");

            List<JObject> testList = new List<JObject>();

            Func<JObject, string> TimeStampFinder = x => (string)x["time"];

            Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>> mockRetQ = new Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>>();
            mockRetQ.Setup(t => t.ExtractElementsOlderThan(It.IsAny<double>())).Returns(testList);
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogAggregator(InputColl, OutputColl, mockRetQ.Object, mockEntryWriter.Object, mockErrorLogger.Object, TimeStampFinder);

            //Act
            obj.SendAggregationsOfAge(4);


            //Assert
            Assert.IsFalse(OutputColl.TryTake(out var tmp));

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Sorts_Output_Ascending()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            var jobj2 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:01.0000000Z\"}");
            var jobj3 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:02.0000000Z\"}");

            List<JObject> testList = new List<JObject>();
            testList.Add(jobj2);
            testList.Add(jobj1);
            testList.Add(jobj3);

            Func<JObject, string> TimeStampFinder = x => (string)x["time"];

            Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>> mockRetQ = new Mock<ES_PS_analyzer.Tools.IRetentionCollection<JObject>>();
            mockRetQ.Setup(t => t.ExtractElementsOlderThan(It.IsAny<double>())).Returns(testList);
            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogAggregator(InputColl, OutputColl, mockRetQ.Object, mockEntryWriter.Object, mockErrorLogger.Object, TimeStampFinder);

            //Act
            obj.SendAggregationsOfAge(4);


            //Assert
            Assert.AreEqual(OutputColl.Take(), jobj1);
            Assert.AreEqual(OutputColl.Take(), jobj2);
            Assert.AreEqual(OutputColl.Take(), jobj3);

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }
    }

}
