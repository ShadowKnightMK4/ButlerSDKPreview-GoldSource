using ButlerSDK;
using ButlerToolContract.DataTypes;
namespace TrenchCoatTests
{
   
    [TestClass]
    public sealed class TrenchCoatUnitTests_SimpleAddSingleMessage_CheckState
    {
        [TestMethod]
        public void TrenchCoatChatCollection_CreateNonNull_Instance()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
        }

        [TestMethod]
        public void TrenchCoatChatCollection_CreateNonNull_MatchesType()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
        }



        [TestMethod]
        public void TrenchCoatChatCollection_AddSysMessage_CountIncreased_ByOne()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message

            ChatCollection.AddSystemMessage("This is a system message.");
            Assert.HasCount(initialCount + 1, ChatCollection);

            Assert.AreEqual(1, ChatCollection.SystemPromptCount);
        }

        [TestMethod]
        public void TrenchCoatChatCollection_AddSysMessage_MessageIsLastMessage()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message

            ChatCollection.AddSystemMessage("This is a system message.");


            // Verify the last message is the system message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.System, lastMessage.Role);
            Assert.AreEqual("This is a system message.", lastMessage.GetCombinedText());
        }

        [TestMethod]
        public void TrenchCoatChatCollection_AddSysMessage_CheckRole()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));

            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message

            ChatCollection.AddSystemMessage("This is a system message.");


            // Verify the last message is the system message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.System, lastMessage.Role);
            Assert.AreEqual("This is a system message.", lastMessage.GetCombinedText());
            Assert.AreEqual(ButlerChatMessageRole.System, lastMessage.Role);
        }

        [TestMethod]
        public void TrenchCoatChatCollection_AddUserMessage_CountIncreased_ByOne()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddUserMessage("This is a user message.");
            Assert.HasCount(initialCount + 1, ChatCollection);
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddUserMessage_MessageIsLastMessage()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddUserMessage("This is a user message.");
            // Verify the last message is the user message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.User, lastMessage.Role);
            Assert.AreEqual("This is a user message.", lastMessage.GetCombinedText());
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddUserMessage_CheckRole()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddUserMessage("This is a user message.");
            // Verify the last message is the user message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.User, lastMessage.Role);
            Assert.AreEqual("This is a user message.", lastMessage.GetCombinedText());

            Assert.AreEqual(ButlerChatMessageRole.User, lastMessage.Role);
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddAssistantMessage_CountIncreased_ByOne()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddAssistantMessage("This is an assistant message.");
            Assert.HasCount(initialCount + 1, ChatCollection);
        }


        [TestMethod]
        public void TrenchCoatChatCollection_AddAssistantMessage_MessageIsLastMessage()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddAssistantMessage("This is an assistant message.");
            // Verify the last message is the user message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.Assistant, lastMessage.Role);
            Assert.AreEqual("This is an assistant message.", lastMessage.GetCombinedText());
        }

        [TestMethod]
        public void TrenchCoatChatCollection_AddAssistantMessage_RoleCheck()
        {
            TrenchCoatChatCollection ChatCollection = new TrenchCoatChatCollection();
            Assert.IsNotNull(ChatCollection);
            Assert.IsInstanceOfType(ChatCollection, typeof(TrenchCoatChatCollection));
            int initialCount = ChatCollection.Count;
            Assert.HasCount(0, ChatCollection); // fail here if non empty message
            ChatCollection.AddAssistantMessage("This is an assistant message.");
            // Verify the last message is the user message we added
            var lastMessage = ChatCollection[ChatCollection.Count - 1];
            Assert.AreEqual(ButlerChatMessageRole.Assistant, lastMessage.Role);
            Assert.AreEqual("This is an assistant message.", lastMessage.GetCombinedText());
            Assert.AreEqual(ButlerChatMessageRole.Assistant, lastMessage.Role);
        }
    }
}
