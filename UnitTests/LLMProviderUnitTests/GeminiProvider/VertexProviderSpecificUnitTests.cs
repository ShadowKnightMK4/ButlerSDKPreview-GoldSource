using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Providers.Gemini;
using ButlerSDK.Providers.Gemini.Vertex;
using ButlerToolContract.DataTypes;
using GenerativeAI.Types;
using Microsoft.VisualBasic;
using SecureStringHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UnitTestDataTypes;
using UnitTests.UnitTestingTools;

namespace ProviderUnitTests_Gemini
{
    [TestClass]
    public class VertexProviderSpecificUnitTests
    {
        
        [TestMethod]
        public void CanCreate_DefaultSettings()
        {
            
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);
            ButlerGeminiProvider inner = testme.GetPrivateField<ButlerGeminiProvider>("_inner");
            Assert.IsNotNull(inner, "Hey check if the inner valiue '_inner' in the vertex provider was changed ");

            var prov_mode = inner.GetType().GetProperty("EndPointMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            ButlerGeminiProvider.GoogleEndPointMode prov_type;
            
            
            prov_type = (ButlerSDK.Providers.Gemini.ButlerGeminiProvider.GoogleEndPointMode)  prov_mode.GetValue(inner); ;
           
            Assert.AreEqual(ButlerGeminiProvider.GoogleEndPointMode.VertexAi, prov_type);
            
        }


        [TestMethod]
        public void CanGetModels_DefaultSettings()
        {
            Directory.SetCurrentDirectory(@"C:\Users\SetupUser\AppData\Local\Google\Cloud SDK");
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);
            testme.Initialize();

            var enum_mods = testme.SupportedModels;

            // as the aplication does it, this shim should too
            Assert.IsNotNull(enum_mods);

            Assert.IsInstanceOfType(enum_mods, typeof(IButlerChatCreationSupportedModels));


            int c = 0;
            List<string> tmp = new();
            foreach (string model in enum_mods.GetEnumerator() )
            {
                Console.WriteLine(model);
                tmp.Add(model);
                c++;
            }
           
        }

        [TestMethod]
        public void CanGetChatFactory_DefaultSettings()
        {
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);

            Assert.IsNotNull(testme.ChatCreationProvider);
        }

        [TestMethod]
        public void CanGetChatFromFactory_Default()
        {
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);

            Assert.IsNotNull(testme.ChatCreationProvider);
            testme.Initialize();

            var chat = testme.ChatCreationProvider.GetChatClient("models/gemini-flash-latest", testme.DefaultOptions, null);

            Assert.IsNotNull(chat);
        }

        [TestMethod]
        public void CanCreateTool_DefaultSettings()
        {
            var testme = new VertexGeminiProvider();
            var tool = new NoSideEffectsTool(null);

            Assert.IsNotNull(testme.CreateChatTool(tool));
        }

        [TestMethod]
        public void CanCreateTool_ThrowsNullOnNull()
        {
            var testme = new VertexGeminiProvider();
            Assert.Throws<ArgumentNullException>(() =>
            {
                Assert.Fail(testme.CreateChatTool(null).ToString());
            });
        }


        [TestMethod]
        public void CanInitializeNoKey_Default()
        {
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);
            testme.Initialize();
        }


        [TestMethod]
        public void CanInitializeYesKey_Default()
        {
            var stub = new SecureString();
            stub.AssignStringThenReadOnly("DUMMY");
            var testme = new VertexGeminiProvider();
            Assert.IsNotNull(testme);
            testme.Initialize(stub);
        }
        
    }
}
