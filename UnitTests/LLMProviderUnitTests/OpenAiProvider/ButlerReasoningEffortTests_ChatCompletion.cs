using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;
#pragma warning disable OPENAI001
namespace UnitTests.Provider.OpenAI
{

    [TestClass]
    public class ButlerReasoningEffortTests_ButlerToOpenAI
    {

        [TestMethod]
        public void TranslatorReject_BadData()
        {
            Assert.Throws<NotImplementedException>(() => { TranslatorReasoningEffortChatCompletion.TranslateToProvider((ButlerThinkingEffortChoice)int.MaxValue); });
        }
        [TestMethod]
        public void TranslatorMapping_ChatCompletion_high_Begets_High()
        {
            Assert.AreEqual(

                TranslatorReasoningEffortChatCompletion.TranslateToProvider(ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.High),
                ChatReasoningEffortLevel.High);


        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_max_Begets_High()
        {
            Assert.AreEqual(

                TranslatorReasoningEffortChatCompletion.TranslateToProvider(ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.Max),
                ChatReasoningEffortLevel.High);


        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_mid_Begets_mid()
        {
            Assert.AreEqual(

                TranslatorReasoningEffortChatCompletion.TranslateToProvider(ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.Medium),
                ChatReasoningEffortLevel.Medium);


        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_minimal_Begets_low()
        {
            Assert.AreEqual(

                TranslatorReasoningEffortChatCompletion.TranslateToProvider(ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.Low),
                ChatReasoningEffortLevel.Low);


        }


        [TestMethod]
        public void TranslatorMapping_ChatCompletion_zero_is_none()
        {
            Assert.AreEqual(

                TranslatorReasoningEffortChatCompletion.TranslateToProvider(ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.None),
                ChatReasoningEffortLevel.None);


        }
    }


    [TestClass]
    public class ButlerReasoningEffortTests_OpenAiToButler
    {
        /* seems the compiler and the typing prevent shoving invalid data, sweet!
        public void TranlsatorRejectsInvalidNumber()
        {
            Assert.Throws<NotImplementedException>(()=> 
            {
                TranslatorReasoningEffortChatCompletion.TranslateFromProvider((ChatReasoningEffortLevel)(-1));
            });
        }
        Uncommenting this will syntax error
        */
        [TestMethod]
        public void TranslatorMapping_ChatCompletion_high_Begets_High()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.High, TranslatorReasoningEffortChatCompletion.TranslateFromProvider(ChatReasoningEffortLevel.High));


        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_mid_Begets_mid()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.Medium, TranslatorReasoningEffortChatCompletion.TranslateFromProvider(ChatReasoningEffortLevel.Medium));

        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_minimal_Begets_low()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.Low, TranslatorReasoningEffortChatCompletion.TranslateFromProvider(ChatReasoningEffortLevel.Low));

        }

        [TestMethod]
        public void TranslatorMapping_ChatCompletion_zero_is_none()
        {
            Assert.AreEqual(ButlerThinkingEffortChoice.None, TranslatorReasoningEffortChatCompletion.TranslateFromProvider(ChatReasoningEffortLevel.None));


        }
    }
}
