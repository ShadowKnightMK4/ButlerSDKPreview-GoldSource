using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrenchCoatTests
{
    internal class MockPromptInjection : IButlerToolPromptInjection
    {
        public string ToolSystemDirectionText = "Mock Prompt Injection System Direction Text.";
        public string GetToolSystemDirectionText()
        {
            return ToolSystemDirectionText;
        }
    }
    [TestClass]
    public sealed class TrenchCoatUnitTest_SystemMessageOrderingTests
    {
        [TestMethod]
        public void TrenchCoatChatCollection_AddSystemFirst_AddUserSecond_VerifySystemIsFirstOrder()
        {
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            ChatCollection.AddSystemMessage("System message 1.");
            ChatCollection.AddUserMessage("User message 1.");

            Assert.HasCount(2, ChatCollection);

            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);


            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.System, firstMessage.Role);
            Assert.AreEqual("System message 1.", firstMessage.GetCombinedText());
            var secondMessage = ChatCollection[1];
            Assert.AreEqual(ButlerChatMessageRole.User, secondMessage.Role);
            Assert.AreEqual("User message 1.", secondMessage.GetCombinedText());
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddSystemSecond_AddUserFirst_VerifySystemIsFirstOrder()
        {
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            ChatCollection.AddUserMessage("User message 1.");
            ChatCollection.AddSystemMessage("System message 1.");


            Assert.HasCount(2, ChatCollection);

            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);




            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.System, firstMessage.Role);
            Assert.AreEqual("System message 1.", firstMessage.GetCombinedText());


            var secondMessage = ChatCollection[1];
            Assert.AreEqual(ButlerChatMessageRole.User, secondMessage.Role);
            Assert.AreEqual("User message 1.", secondMessage.GetCombinedText());
        }

        [TestMethod]
        public void TrenchCoatChatCollection_AddUserMessage_NoSystemPrompt_VerifySystemCount()
        {
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            ChatCollection.AddUserMessage("User message 1.");

            Assert.HasCount(1, ChatCollection);
            Assert.AreEqual(0, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);

            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.User, firstMessage.Role);
            Assert.AreEqual("User message 1.", firstMessage.GetCombinedText());
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddSystemMessage_AddPromptInject_SystemPromptIsFirst()
        {
            MockPromptInjection MockMe = new();
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            ChatCollection.AddSystemMessage("System message 1.");
            ChatCollection.AddPromptInjectionMessage(new ButlerSystemChatMessage("Prompt injection message 1."), MockMe);


            Assert.HasCount(2, ChatCollection);
            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.PromptInjectionCount);

            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.System, firstMessage.Role);
            Assert.AreEqual("System message 1.", firstMessage.GetCombinedText());
            var secondMessage = ChatCollection[1];
            Assert.AreEqual(ButlerChatMessageRole.System, secondMessage.Role);
            Assert.AreEqual("Prompt injection message 1.", secondMessage.GetCombinedText());

        }

        [TestMethod]

        public void TrenchCoatChatCollection_AddPromptInject_AddSystemMessage_VerifyOrderOK_OnRandomAddAlt()
        {
            MockPromptInjection MockMe = new();
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            ChatCollection.AddUserMessage("User message 1.");
            ChatCollection.AddPromptInjectionMessage(new ButlerSystemChatMessage("Prompt injection message 1."), MockMe);


            ChatCollection.AddSystemMessage("System message 1.");


            Assert.HasCount(3, ChatCollection   );
            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.PromptInjectionCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);

            Console.WriteLine("NOTE THIS TEST Checks if the order of messages is #1 System, #2 Prompt Injection, #3 User Message. If Added in that order");
            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.System, firstMessage.Role);
            Assert.AreEqual("System message 1.", firstMessage.GetCombinedText());
            var secondMessage = ChatCollection[1];
            Assert.AreEqual(ButlerChatMessageRole.System, secondMessage.Role);
            Assert.AreEqual("Prompt injection message 1.", secondMessage.GetCombinedText());
            var thirdMessage = ChatCollection[2];
            Assert.AreEqual(ButlerChatMessageRole.User, thirdMessage.Role);
            Assert.AreEqual("User message 1.", thirdMessage.GetCombinedText());
        }


        [TestMethod]

        public void TrenchCoatChatCollection_AddPromptInject_AddSystemMessage_VerifyOrderOK_OnRandomAdd()
        {
            MockPromptInjection MockMe = new();
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            ChatCollection.AddPromptInjectionMessage(new ButlerSystemChatMessage("Prompt injection message 1."), MockMe);

            ChatCollection.AddUserMessage("User message 1.");

            ChatCollection.AddSystemMessage("System message 1.");


            Assert.HasCount(3, ChatCollection);
            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.PromptInjectionCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);

            Console.WriteLine("NOTE THIS TEST Checks if the order of messages is #1 System, #2 Prompt Injection, #3 User Message. If Added in that order");
            var firstMessage = ChatCollection[0];
            Assert.AreEqual(ButlerChatMessageRole.System, firstMessage.Role);
            Assert.AreEqual("System message 1.", firstMessage.GetCombinedText());
            var secondMessage = ChatCollection[1];
            Assert.AreEqual(ButlerChatMessageRole.System, secondMessage.Role);
            Assert.AreEqual("Prompt injection message 1.", secondMessage.GetCombinedText());
            var thirdMessage = ChatCollection[2];
            Assert.AreEqual(ButlerChatMessageRole.User, thirdMessage.Role);
            Assert.AreEqual("User message 1.", thirdMessage.GetCombinedText());
        }
        
        
        [TestMethod]
        public void TrenchCoatChatCollection_AddPromptInject_AddSystemMessage_AddUserMessage_VerifyOrder()
        {
            MockPromptInjection MockMe = new();
            TrenchCoatChatCollection ChatCollection = new();
            Assert.IsNotNull(ChatCollection);
            Debugger.Break();
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            ChatCollection.AddPromptInjectionMessage(new ButlerSystemChatMessage("Prompt injection message 1."), MockMe);
            ChatCollection.AddSystemMessage("System message 1.");
            ChatCollection.AddUserMessage("User message 1.");

            Assert.HasCount(3, ChatCollection);
            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
            Assert.AreEqual(1, ChatCollection.PromptInjectionCount);
            Assert.AreEqual(1, ChatCollection.RunningContextWindowCount);

            
            Console.WriteLine("NOTE THIS TEST Checks if the order of messages is #1 System, #2 Prompt Injection, #3 User Message. If Added in that order");

        }
    }


}
