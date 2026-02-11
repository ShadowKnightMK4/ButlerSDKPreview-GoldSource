using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Provider.OpenAI
{
    /// <summary>
    /// strictly speaking these tests are linked to <see cref="TranslatorChatMessage_Tests"/>. Ie if the tranlsator is broken there, these will break too
    /// </summary>
    [TestClass]
    public class TranslatorChatLog_Tests
    {
        List<ButlerChatMessage> Msgs = new();
        List<ChatMessage> ProviderMessages = new();
        [TestInitialize]
        public void ConfigureCannedMessages()
        {
            // message 1
            Msgs.Add(new ButlerChatMessage("You are Helpful"));
            // message 2
            Msgs.Add(new ButlerSystemChatMessage("SECRETLY YOU ENJOY Helping and you are joyouse red Oni about it rathan than blue Oni"));

            // message 3
            Msgs.Add(new ButlerChatToolCallMessage("TESTCALL", "DONOTHING", ""));
            // message 4
            Msgs.Add(new ButlerChatToolResultMessage("TESTCALL", "The result of nothing was NULL."));

            // message 5
            Msgs.Add(new ButlerChatToolCallMessage("TESTCALL2", "FetchTheWorkChicken", ""));
            // Message 6
            Msgs.Add(new ButlerChatToolResultMessage("TESTCALL2", "Chicken"));

            // Message 7
            Msgs.Add(new ButlerAssistantChatMessage("I found the word chicken and did nothing. *giggles* anything else?")); 


            

        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_Message7_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);

            Assert.IsNotNull(Provider);
            AssistantChatMessage msg = (AssistantChatMessage)Provider[6];
            Assert.IsNotNull(msg);

            Assert.IsTrue(msg is AssistantChatMessage);
            Assert.HasCount(1,msg.Content);

            Assert.AreEqual(ChatMessageContentPartKind.Text, msg.Content[0].Kind);
            Assert.AreEqual("I found the word chicken and did nothing. *giggles* anything else?", msg.Content[0].Text );
        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_Message6_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);
            var msg4 = Provider[5];
            var checkme = msg4 as ToolChatMessage;
            Assert.IsNotNull(msg4);

            Assert.IsNotNull(checkme); // note: Openai stuffs its toool calls into assistmeness
                                            // butler pulls them out and pairs toolcall (
                                            // aka the request in its own messagee mark && the tool
                                            // result following

            Assert.AreEqual("TESTCALL2", checkme.ToolCallId );
            Assert.AreEqual("Chicken",checkme.Content[0].Text);
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message5_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);
            var msg3 = Provider[4];
            var checkme = msg3 as AssistantChatMessage;
            Assert.IsNotNull(msg3);

            Assert.IsNotNull(checkme); // note: Openai stuffs its toool calls into assistmeness
                                            // butler pulls them out and pairs toolcall (
                                            // aka the request in its own messagee mark && the tool
                                            // result following

            Assert.HasCount(1, checkme.ToolCalls);
            Assert.AreEqual("TESTCALL2", checkme.ToolCalls[0].Id);
            Assert.AreEqual("FetchTheWorkChicken", checkme.ToolCalls[0].FunctionName);
            Assert.IsEmpty(checkme.Content);
        }



        [TestMethod]
        public void TranslatorToProvide_GenericList_Message4_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);
            var msg4 = Provider[3];
            var checkme = msg4 as ToolChatMessage;
            Assert.IsNotNull(msg4);

            Assert.IsNotNull(checkme); // note: Openai stuffs its toool calls into assistmeness
                                            // butler pulls them out and pairs toolcall (
                                            // aka the request in its own messagee mark && the tool
                                            // result following

            Assert.AreEqual("TESTCALL", checkme.ToolCallId);
            Assert.AreEqual("The result of nothing was NULL.", checkme.Content[0].Text);
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message3_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);
            var msg3 = Provider[2];
            var checkme = msg3 as AssistantChatMessage;
            Assert.IsNotNull(msg3);

            Assert.IsNotNull(checkme); // note: Openai stuffs its toool calls into assistmeness
                                                                 // butler pulls them out and pairs toolcall (
                                                                 // aka the request in its own messagee mark && the tool
                                                                 // result following

            Assert.HasCount(1,checkme.ToolCalls);
            Assert.AreEqual("TESTCALL", checkme.ToolCalls[0].Id);
            Assert.AreEqual("DONOTHING", checkme.ToolCalls[0].FunctionName);
            Assert.IsEmpty(checkme.Content);
        }
        [TestMethod]    
        public void TranslatorToProvide_GenericList_Message2_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);
            var msg2 = Provider[1];

            Assert.IsNotNull(msg2);
            Assert.IsTrue(msg2 is SystemChatMessage);
            Assert.HasCount(1,msg2.Content );
            Assert.AreEqual(ChatMessageContentPartKind.Text,msg2.Content[0].Kind );
            Assert.AreEqual(Msgs[1].Content[0].Text, msg2.Content[0].Text);
            

        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_CountMatches()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);

            Assert.IsNotNull(Provider);
            Assert.HasCount(7, Provider);
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message1_check()
        {
            var Provider = TranslatorChatLog.TranslateToProvider(Msgs);

            Assert.IsNotNull(Provider);
            ChatMessage msg = (ChatMessage)Provider[0];
            Assert.IsNotNull(msg);

            Assert.IsTrue(msg is UserChatMessage);
            Assert.HasCount(1, msg.Content);

            Assert.AreEqual(ChatMessageContentPartKind.Text, msg.Content[0].Kind);
            Assert.AreEqual(msg.Content[0].Text, Msgs[0].Content[0].Text);


        }
        [TestMethod]
        public void TranslateToProvderEmpty_ShouldProduceEmptyProviderResult()
        {
            var Generic = new List<ButlerChatMessage>();
            Assert.IsNotNull(Generic);
            Assert.IsEmpty(Generic);
            var Translated = TranslatorChatLog.TranslateToProvider(Generic);
            Assert.IsNotNull(Translated);

            Assert.IsEmpty(Translated);
        }

        [TestMethod]
        public void TranslatorToProvide_NullList_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => {
                var _ = TranslatorChatLog.TranslateToProvider(null!); // supposed to be null - null throws exception
            });
        }

    
    }
}
