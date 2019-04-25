using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Moq;

namespace UnitTest_ES_PS
{
    [TestClass]
    public class RetentionCollection
    {
        [TestMethod]
        public void Parses_Correct_DateTime_String()
        {
            //Arrange
            var insertObj = JObject.Parse("{\"time\":\"2019-10-12T07:00:00.0000000Z\"}");

            Mock<ES_PS_analyzer.Tools.ITimeProvider> mockTime = new Mock<ES_PS_analyzer.Tools.ITimeProvider>();
            Func<string, DateTime> mockTimeParse = x => DateTime.Parse(x);
            mockTime.Setup(x => x.Parse(It.IsAny<string>())).Returns(mockTimeParse);

            var obj = new ES_PS_analyzer.Tools.RetentionCollection<JObject>(mockTime.Object, x => (string)x["time"]);

            //Act
            obj.InsertElement(insertObj);

            //Assert
            mockTime.Verify(x => x.Parse(It.Is<string>(y => y == "10/12/2019 07:00:00")));
            mockTime.VerifyNoOtherCalls();

        }

        [TestMethod]
        public void Filters_One_Correctly()
        {
            //Arrange
            var insertObj = JObject.Parse("{\"time\":\"2019-10-12T07:00:00.0000000Z\"}");

            Mock<ES_PS_analyzer.Tools.ITimeProvider> mockTime = new Mock<ES_PS_analyzer.Tools.ITimeProvider>();
            mockTime.Setup(x => x.Now()).Returns(DateTime.Parse("10/12/2019 07:00:10"));
            Func<string, DateTime> mockTimeParse = x => DateTime.Parse(x);
            mockTime.Setup(x => x.Parse(It.IsAny<string>())).Returns(mockTimeParse);

            var obj = new ES_PS_analyzer.Tools.RetentionCollection<JObject>(mockTime.Object, x => (string)x["time"]);

            //Act
            obj.InsertElement(insertObj);
            var emptyRes = obj.ExtractElementsOlderThan(10);
            var allRes = obj.ExtractElementsOlderThan(9);
            var emptyres2 = obj.ExtractElementsOlderThan(0);

            //Assert
            mockTime.Verify(x => x.Now());

            Assert.AreEqual(emptyRes.Count, 0);
            Assert.AreEqual(allRes.Count, 1);
            Assert.AreEqual(allRes[0], insertObj);
            Assert.AreEqual(emptyres2.Count, 0);

        }

        [TestMethod]
        public void Filters_Multiple_Correctly()
        {
            //Arrange
            var insertObj = JObject.Parse("{\"time\":\"2019-10-12T07:00:00.0000000Z\"}");
            var insertObj2 = JObject.Parse("{\"time\":\"2019-10-12T07:00:01.0000000Z\"}");
            var insertObj3 = JObject.Parse("{\"time\":\"2019-10-12T07:00:02.0000000Z\"}");
            var insertObj4 = JObject.Parse("{\"time\":\"2019-10-12T07:00:03.0000000Z\"}");

            Mock<ES_PS_analyzer.Tools.ITimeProvider> mockTime = new Mock<ES_PS_analyzer.Tools.ITimeProvider>();
            mockTime.Setup(x => x.Now()).Returns(DateTime.Parse("10/12/2019 07:00:10"));
            Func<string, DateTime> mockTimeParse = x => DateTime.Parse(x);
            mockTime.Setup(x => x.Parse(It.IsAny<string>())).Returns(mockTimeParse);

            var obj = new ES_PS_analyzer.Tools.RetentionCollection<JObject>(mockTime.Object, x => (string)x["time"]);

            //Act
            obj.InsertElement(insertObj);
            obj.InsertElement(insertObj2);
            obj.InsertElement(insertObj3);
            obj.InsertElement(insertObj4);

            var res3 = obj.ExtractElementsOlderThan(7);
            var emptyRes = obj.ExtractElementsOlderThan(7);          
            var res1 = obj.ExtractElementsOlderThan(6);

            //Assert
            mockTime.Verify(x => x.Now());

            Assert.AreEqual(emptyRes.Count, 0);
            Assert.AreEqual(res3.Count, 3);
            Assert.AreEqual(res1.Count, 1);

            Assert.IsTrue(res3.Contains(insertObj));
            Assert.IsTrue(res3.Contains(insertObj2));
            Assert.IsTrue(res3.Contains(insertObj3));
            Assert.IsTrue(res1.Contains(insertObj4));

        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Inserting_Without_TimeStamp_Field_Causes_FormatException()
        {
            //Arrange
            var insertObj = JObject.Parse("{\"times\":\"2019-10-12T07:00:00.0000000Z\"}");

            Mock<ES_PS_analyzer.Tools.ITimeProvider> mockTime = new Mock<ES_PS_analyzer.Tools.ITimeProvider>();
            Func<string, DateTime> mockTimeParse = x => DateTime.Parse(x);
            mockTime.Setup(x => x.Parse(It.IsAny<string>())).Throws(new Exception());

            var obj = new ES_PS_analyzer.Tools.RetentionCollection<JObject>(mockTime.Object, x => (string)x["time"]);

            //Act
            obj.InsertElement(insertObj);

            //Assert

        }
    }
}
