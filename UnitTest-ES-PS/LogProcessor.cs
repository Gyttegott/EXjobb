using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Moq;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class LogProcessor
    {
        [TestMethod]
        public void Parsed_PSInfo_Is_Inserted_Into_First_Level_Cache_With_Correct_Host_name_And_Risk()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            /*Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };*/
            ES_PS_analyzer.PSInfo testObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej"
            };
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => testObj;
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null)).Verifiable();

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x.computer_name == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == null))).Returns(400);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder);

            //Act
            obj.ProcessLog();


            //Assert
            mockCache1.Verify(t => t.SetLastCommand(It.Is<string>(x => x == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == testObj && x.powershell_risk == 400)));
            mockCache1.Verify();
            mockCache1.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Same_Log_Is_Outputted_With_Added_Risk()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            /*Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };*/
            ES_PS_analyzer.PSInfo testObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej"
            };
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => testObj;
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null));

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x.computer_name == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == null))).Returns(400);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder);

            //Act
            obj.ProcessLog();
            var output = OutputColl.Take();


            //Assert
            Assert.AreEqual(jobj1, output);
            Assert.AreEqual(400, (double)output["risk"]);

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Calculator_Is_Called_With_Null_As_Last_Command_When_Not_Found_In_Cache()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            /*Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };*/
            ES_PS_analyzer.PSInfo testObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej"
            };
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => testObj;
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null));

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x.computer_name == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == null))).Returns(400).Verifiable();

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder);

            //Act
            obj.ProcessLog();

            //Assert
            mockCalc.Verify();
            mockCalc.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Calculator_Is_Called_With_Last_Command_When_Found_In_Cache()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            /*Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };*/
            ES_PS_analyzer.PSInfo testObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej"
            };
            ES_PS_analyzer.PSInfo testCacheObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej2"
            };
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => testObj;
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(testCacheObj));

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x == testObj), It.Is<ES_PS_analyzer.PSInfo>(x => x == testCacheObj))).Returns(400).Verifiable();

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder);

            //Act
            obj.ProcessLog();

            //Assert
            mockCalc.Verify();
            mockCalc.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Second_Cache_Is_Called_When_First_Gives_Null()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"hej\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            /*Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };*/
            ES_PS_analyzer.PSInfo testObj = new ES_PS_analyzer.PSInfo
            {
                computer_name = "hej"
            };
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => testObj;
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache2 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null));
            mockCache2.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null)).Verifiable();

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x.computer_name == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == null))).Returns(400).Verifiable();

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder, mockCache2.Object);

            //Act
            obj.ProcessLog();

            //Assert
            mockCache2.Verify();
            mockCache2.VerifyNoOtherCalls();

            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Empty_Host_Name_Gives_Error_And_Dropped_Log()
        {
            //Arrange
            var InputColl = new BlockingCollection<JObject>();
            var OutputColl = new BlockingCollection<JObject>();
            var jobj1 = JObject.Parse("{\"hej\":\"\",\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            Func<JObject, ES_PS_analyzer.PSInfo> InfoParser = x => new ES_PS_analyzer.PSInfo
            {
                computer_name = (string)x["hej"]
            };
            Action<JObject, double> RiskAdder = (x, y) => x["risk"] = y;

            InputColl.Add(jobj1);

            Mock<ES_PS_analyzer.Tools.ICommandCache> mockCache1 = new Mock<ES_PS_analyzer.Tools.ICommandCache>();
            mockCache1.Setup(t => t.GetLastCommand(It.Is<string>(x => x == "hej"))).Returns(Task.FromResult<ES_PS_analyzer.PSInfo>(null));

            Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator> mockCalc = new Mock<ES_PS_analyzer.RiskEvaluation.IRiskCalculator>();
            mockCalc.Setup(t => t.CalculateRisk(It.Is<ES_PS_analyzer.PSInfo>(x => x.computer_name == "hej"), It.Is<ES_PS_analyzer.PSInfo>(x => x == null))).Returns(400);

            Mock<ES_PS_analyzer.Tools.IErrorLogHandler> mockErrorLogger = new Mock<ES_PS_analyzer.Tools.IErrorLogHandler>();
            Mock<ES_PS_analyzer.Tools.IEntryContentWriter> mockEntryWriter = new Mock<ES_PS_analyzer.Tools.IEntryContentWriter>();

            var obj = new ES_PS_analyzer.LogProcessor(InputColl, OutputColl, mockCache1.Object, mockCalc.Object, mockEntryWriter.Object, mockErrorLogger.Object, InfoParser, RiskAdder);

            //Act
            obj.ProcessLog();

            //Assert
            mockErrorLogger.Verify(t => t.LogError(It.IsAny<string>()));
            mockEntryWriter.Verify(t => t.WriteContent(It.IsAny<string>(), It.IsAny<byte[]>()));
            mockEntryWriter.Verify(t => t.GetStorageDescription(It.IsAny<string>()));
            mockErrorLogger.VerifyNoOtherCalls();
            mockEntryWriter.VerifyNoOtherCalls();

            Assert.IsFalse(OutputColl.TryTake(out var res));
        }
    }

}
