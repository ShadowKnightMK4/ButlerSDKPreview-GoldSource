using ButlerSDK.Providers.OpenAI;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerToolContract.DataTypes;
#pragma warning disable OPENAI001
namespace ProviderUnitTests_Openai
{


    [TestClass]
    public class ButlerReasoningEffortTests_Response
    {

        [TestMethod]
        public void TestFromOpenAiToButler_High_ToHigh()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.High,

                TranslatorReasoningEffortResponse.TranslateFromProvider(ResponseReasoningEffortLevel.High));
        }


        [TestMethod]
        public void TestFromOpenAiToButler_Low_To_Low()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.Low,

                TranslatorReasoningEffortResponse.TranslateFromProvider(ResponseReasoningEffortLevel.Low));
        }

        [TestMethod]
        public void TestFromButlerToOpenAI_Max_to_High()
        {
            Assert.AreEqual(
                ResponseReasoningEffortLevel.High,

                TranslatorReasoningEffortResponse.TranslateToProvider(ButlerThinkingEffortChoice.Max));
        }

        [TestMethod]
        public void TestFromButlerToOpenAI_High_to_High()
        {
            Assert.AreEqual(
                ResponseReasoningEffortLevel.High,

                TranslatorReasoningEffortResponse.TranslateToProvider(ButlerThinkingEffortChoice.High));
        }

        [TestMethod]
        public void TestFromButlerToOpenAI_Mid_To_Mid()
        {
            Assert.AreEqual(
                ResponseReasoningEffortLevel.Medium,

                TranslatorReasoningEffortResponse.TranslateToProvider(ButlerThinkingEffortChoice.Medium));
        }

        [TestMethod]
        public void TestFromButlerToOpenAI_Low_to_Low()
        {
            Assert.AreEqual(
                ResponseReasoningEffortLevel.Low,

                TranslatorReasoningEffortResponse.TranslateToProvider(ButlerThinkingEffortChoice.Low));
        }


        [TestMethod]
        public void TestFromButlerToOpenAI_None_ToNone()
        {
            Assert.AreEqual(
                ResponseReasoningEffortLevel.None,


                TranslatorReasoningEffortResponse.TranslateToProvider(ButlerThinkingEffortChoice.None));
        }
    }
}
