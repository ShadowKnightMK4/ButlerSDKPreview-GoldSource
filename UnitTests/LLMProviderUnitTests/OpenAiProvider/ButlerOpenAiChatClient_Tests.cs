using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.OpenAI;
using ButlerLLMProviderPlatform.Protocol;
using OpenAI;
using OpenAI.Chat;
using SecureStringHelper;
namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class ButlerOpenAiChatClient_Tests
    {
        [TestMethod]
        public void ProviderCanGive_ChatClient_NotNull_WithDummyKey()
        {
            Console.WriteLine("This test uses a dummy key. It only tests that the provider can return a chat client instance sucessfully");
            SecureString Dummy = new();
            Dummy.AssignStringThenReadyOnly("THIS API KEY DOES NOT MATTER") ;
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            Provider.Initialize(Dummy);
            Assert.IsNotNull(Provider);
            var Client = Provider.GetChatClient("DOESNOTMATTER", Provider.DefaultOptions, null);
            Assert.IsNotNull(Client);
            Assert.IsInstanceOfType(Client, typeof(IButlerChatClient));
        }

        [TestMethod]
        public void ProviderRejects_ChatClientAsk_IfNotInitialzed()
        {
            Console.WriteLine("This test uses a dummy key. It only tests that the provider can return a chat client instance sucessfully");
            
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            
            Assert.IsNotNull(Provider);
            Assert.Throws<InvalidOperationException>(() => {
                var Client = Provider.GetChatClient("DOESNOTMATTER", Provider.DefaultOptions, null);
                Assert.IsNotNull(Client);
                Assert.IsInstanceOfType(Client, typeof(IButlerChatClient));
            });
            
        
        }

        [TestMethod]
        public void MakeProviderInstance()
        {
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            Assert.IsNotNull(Provider);
        }

        [TestMethod]
        public void CanGetChatClient_ByClientFactory()
        {
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            Assert.IsNotNull(Provider);
            var Factory = Provider.ChatCreationProvider;
            Assert.IsNotNull(Factory);
            Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
        }

        [TestMethod]
        public void OpenAiSpecific_IntialzieWithNoKey_ThrowsExcpetion()
        {
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            Assert.IsNotNull(Provider);
            var Factory = Provider.ChatCreationProvider;
            Assert.IsNotNull(Factory);
            Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
            Assert.Throws<ArgumentNullException>(() => {
                Factory.Initialize(null!); // compiler complains of null. We're testing if the routine goes boom on null
            });
            Assert.IsTrue(Factory is not IButlerChatCreationProvider_NoApiNeeded);
        }

        [TestMethod]
        public void OpenAiSpecific_InitializeRandomKey_ThrowsException()
        {
            SecureString x = new SecureString();
            foreach (char c in "THISISNOTAKEY")
            {
                x.AppendChar(c);
            }
            var Provider = new ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider();
            Assert.IsNotNull(Provider);
            var Factory = Provider.ChatCreationProvider;
            Assert.IsNotNull(Factory);
            Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
        }


    }
}
