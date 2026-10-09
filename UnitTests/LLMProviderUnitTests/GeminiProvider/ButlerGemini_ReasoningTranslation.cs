using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.Gemini;
using ButlerToolContract.DataTypes;
using GenerativeAI.Types;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
namespace ProviderUnitTests_Gemini
{
    [TestClass]
    public class ButlerGeimini_ReasoningEffort_FromGemini
    {
        [TestMethod]
        public void NoThinking_SetIsNull()
        {
            var Config = new GenerationConfig();
            Config.ThinkingConfig = null;
            Assert.IsNull(TranslatorChatReasoningEffort.FromProvider(Config.ThinkingConfig));
        }

    
        [TestMethod]
        public void MinThinking_SetMinResult()
        {
            var Config = new GenerationConfig();
            Config.ThinkingConfig = new ThinkingConfig();
            Config.ThinkingConfig.ThinkingLevel = ThinkingLevel.LOW;
            var result = TranslatorChatReasoningEffort.FromProvider(Config.ThinkingConfig);
            Assert.IsNotNull(result);
            Assert.AreEqual(ButlerThinkingEffortChoice.Low, result);
        }


        [TestMethod]
        public void HighThinking_SetHighResult()
        {
            var Config = new GenerationConfig();
            Config.ThinkingConfig = new ThinkingConfig();
            Config.ThinkingConfig.ThinkingLevel = ThinkingLevel.HIGH;
            var result = TranslatorChatReasoningEffort.FromProvider(Config.ThinkingConfig);
            Assert.IsNotNull(result);
            Assert.AreEqual(ButlerThinkingEffortChoice.Max, result);
        }


        [TestMethod]
        public void UnSetThinking_SetNullResult()
        {
            var Config = new GenerationConfig();
            Config.ThinkingConfig = new ThinkingConfig();
            Config.ThinkingConfig.ThinkingLevel = ThinkingLevel.THINKING_LEVEL_UNSPECIFIED;
            var result = TranslatorChatReasoningEffort.FromProvider(Config.ThinkingConfig);
            Assert.IsNull(result);
        }
    }

    [TestClass]
    public class ButlerGemini_ReasoningEffort_ToGemini
    {

        [TestMethod]
        public void Rand1ButlerMeansNullGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = (ButlerThinkingEffortChoice)int.MaxValue;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNull(Effort);
        }

        [TestMethod]
        public void Rand2ButlerMeansNullGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = (ButlerThinkingEffortChoice)int.MinValue;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNull(Effort);
        }

        [TestMethod]
        public void NUllButler_MeansNullGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = null;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);
            Assert.IsNull(Effort);
        }



        [TestMethod]
        public void MaxButler_MeansHighGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = ButlerThinkingEffortChoice.Max;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNotNull(Effort);
            Assert.IsNotNull(Effort.ThinkingLevel);
            Assert.AreEqual(ThinkingLevel.HIGH, Effort.ThinkingLevel);
        }

         
        [TestMethod]
        public void HighButler_MeansHighGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = ButlerThinkingEffortChoice.High;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNotNull(Effort);
            Assert.IsNotNull(Effort.ThinkingLevel);
            Assert.AreEqual(ThinkingLevel.HIGH, Effort.ThinkingLevel);
        }

            
        [TestMethod]
        public void LowButler_MeansLowGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = ButlerThinkingEffortChoice.Low;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNotNull(Effort);
            Assert.IsNotNull(Effort.ThinkingLevel);
            Assert.AreEqual(ThinkingLevel.LOW, Effort.ThinkingLevel);
        }


        [TestMethod]
        public void MidButler_MeansLowGemini()
        {
            ButlerChatCompletionOptions Test = new();
            Test.ReasoningEffort = ButlerThinkingEffortChoice.Medium;
            var Effort = TranslatorChatReasoningEffort.TranslateToProvider(Test);

            Assert.IsNotNull(Effort);
            Assert.IsNotNull(Effort.ThinkingLevel);
            Assert.AreEqual(ThinkingLevel.LOW, Effort.ThinkingLevel);
        }
    }
}
