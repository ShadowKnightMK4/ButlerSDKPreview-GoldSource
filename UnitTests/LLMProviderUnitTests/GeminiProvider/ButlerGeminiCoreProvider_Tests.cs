using ButlerSDK.Providers.Gemini;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using GenerativeAI.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UnitTests.UnitTestingTools;
namespace GeminiProvider
{
    [TestClass]
    public class ButlerGeminiCoreProvider_Tests
    {
        [TestMethod]
        public void CanActuallyMakeInstance_IsNotNUll()
        {
            var x = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
            Assert.IsNotNull(x);
            Assert.IsInstanceOfType(x, typeof(IButlerLLMProvider));
        }
        [TestMethod]
        public void ChatCreationProviderTest_ShouldNotBeNull()
        {
            ButlerGeminiProvider x = new ButlerGeminiProvider();
            Assert.IsNotNull(x);
            Assert.IsInstanceOfType(x, typeof(IButlerLLMProvider));
        }

        [TestMethod]
        public void CreateChatToolWorks_ShouldMatch()
        {
            ButlerGeminiProvider x = new();
            Assert.IsNotNull(x);
            IButlerToolBaseInterface testtool = new NoSideEffectsTool(null);
            Assert.IsNotNull(testtool);

            FunctionDeclaration tool = (FunctionDeclaration)x.CreateChatTool(testtool);

            Assert.IsNotNull(tool);
            Assert.IsInstanceOfType(tool, typeof(FunctionDeclaration));

            Assert.AreEqual(tool.Description, testtool.ToolDescription);
            Assert.AreEqual(tool.Name, testtool.ToolName);

            JsonNode DataJason;
            if (tool.ParametersJsonSchema is not null)
                DataJason = tool.ParametersJsonSchema;
            else
                DataJason = null!; // null here is intended to be failure

            Assert.IsNotNull(DataJason);

            JsonDocument? doc;

            {
                doc = JsonDocument.Parse(testtool.GetToolJsonString());
            }
            var test_against = doc.RootElement.AsNode();


            Assert.IsTrue(JsonNode.DeepEquals(DataJason, test_against));
        }

    }
}
