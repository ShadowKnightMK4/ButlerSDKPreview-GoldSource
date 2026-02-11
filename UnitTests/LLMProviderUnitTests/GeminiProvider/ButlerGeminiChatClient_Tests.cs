using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ButlerLLMProviderPlatform;
using SecureStringHelper;

namespace GeminiProvider

    {
        [TestClass]
        public class ButlerChatClient_Tests
        {
        SecureString dummy = new();
        [TestInitialize]
        public void TestInitialize()
        {
            dummy.AssignStringThenReadyOnly("HELLO");
        }
            [TestMethod]
            public void CanCheckChatClient_ByProvider()
            {

                var Provider = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
                Assert.IsNotNull(Provider);
            Provider.Initialize(dummy);

                var Client = Provider.GetChatClient("DOESNOTMATTER", Provider.DefaultOptions, null);
                Assert.IsNotNull(Client);
                Assert.IsInstanceOfType(Client, typeof(IButlerChatClient));
            }

            [TestMethod]
            public void MakeProviderInstance()
            {
                var Provider = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
            Assert.IsNotNull(Provider);
            }

            [TestMethod]
            public void CanGetChatClient_ByClientFactory()
            {
                var Provider = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
                Assert.IsNotNull(Provider);
                Provider.Initialize(dummy);
                var Factory = Provider.ChatCreationProvider;
                Assert.IsNotNull(Factory);
                Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
            }

            [TestMethod]
            public void GeminiSpecific_IntialzieWithNoKey_ThrowsExcpetion()
            {
                var Provider = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
                Assert.IsNotNull(Provider);
                Provider.Initialize(dummy);
            var Factory = Provider.ChatCreationProvider;
                Assert.IsNotNull(Factory);
                Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
                Assert.Throws<ArgumentNullException>(() => {
                    Factory.Initialize(null!); // compiler complains of null. We're testing if the routine goes boom on null
                });
                Assert.IsTrue(Factory is not IButlerChatCreationProvider_NoApiNeeded);
            }

            [TestMethod]
            public void GeminiSpecific_InitializeRandomKey_ThrowsNoException()
            {
                SecureString x = new SecureString();
                foreach (char c in "THISISNOTAKEY")
                {
                    x.AppendChar(c);
                }
                var Provider = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
                Assert.IsNotNull(Provider);
                var Factory = Provider.ChatCreationProvider;
                Assert.IsNotNull(Factory);
                Assert.IsInstanceOfType(Factory, typeof(IButlerChatCreationProvider));
                 Factory.Initialize(x); 
            }


        }
    }


