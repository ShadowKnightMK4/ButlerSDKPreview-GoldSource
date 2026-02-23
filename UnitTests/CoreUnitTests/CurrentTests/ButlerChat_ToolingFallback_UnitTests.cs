using ApiKeyMgr;
using ButlerProtocolBase.ToolSecurity;
using ButlerSDK;
using ButlerSDK.Core;
using ButlerSDK.Providers.UnitTesting.MockProvider;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using CoreUnitTests.CurrentTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnitTests.UnitTestingTools;

namespace CoreUnitTests
{
    [ToolSurfaceCapabilities(ToolSurfaceScope.NoPermissions)]
    class PromptSteeringToolMock : IButlerToolBaseInterface, IButlerToolPromptInjection, IButlerToolPostCallInjection
    {
        public string ToolName => "PromptSteerTest";

        public string ToolVersion => "K";

        public string ToolDescription => "Does the chat session class trigger ok?";

        public string GetToolJsonString()
        {
            return ButlerToolBase.NoArgJson;
        }

        public string GetToolPostCallDirection()
        {
            return "GO LEFT";
        }

        public string GetToolSystemDirectionText()
        {
            return "GO RIGHT";
        }

        public ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return null;
        }

        public bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            return true;
        }
    }
    [TestClass]
    public class ButlerChat_ToolingFallback_UnitTests
    {
        [TestMethod]
        public void ToolingCheck_TrenchyCheck_NormalWorks()
        {
            InMemoryApiKey EmptyTheVault = new();
            MockProviderEntryPoint Dummy = new();
            Butler Jeeves = new Butler(EmptyTheVault, Dummy, Dummy.DefaultOptions, "NO", Butler.NoApiKey, null, null) ;
            PromptSteeringToolMock SteeringWheel = new();
            Jeeves.AddTool(SteeringWheel);
            Assert.IsTrue(Jeeves.ExistsTool(SteeringWheel.ToolName));
        }

        [TestMethod]
        public void ToolingCheck_NotTrenchy_ThrowsOnAdd_class_PromptSteeringToolMock()
        {
            InMemoryApiKey EmptyTheVault = new();
            MockProviderEntryPoint Dummy = new();
            Butler Jeeves = new Butler(EmptyTheVault, Dummy, Dummy.DefaultOptions, "NO", Butler.NoApiKey, new DummyList(), null, null) ;
            PromptSteeringToolMock SteeringWheel = new();

            Assert.IsTrue(Jeeves.TrenchSupport == ButlerBase.TrenchSupportFallback.Throw);

            Assert.ThrowsException<InvalidOperationException>(() => {
                Jeeves.AddTool(SteeringWheel);
            });



            Assert.IsFalse(Jeeves.ExistsTool(SteeringWheel.ToolName));
        }

        [TestMethod]
        public void ToolingCheck_NotTrenchy_AllowsOnAdd_class_PromptSteeringToolMock()
        {
            InMemoryApiKey EmptyTheVault = new();
            MockProviderEntryPoint Dummy = new();
            Butler Jeeves = new Butler(EmptyTheVault, Dummy, Dummy.DefaultOptions, "NO", Butler.NoApiKey, new DummyList(), null, null);
            PromptSteeringToolMock SteeringWheel = new();

            Assert.IsTrue(Jeeves.TrenchSupport == ButlerBase.TrenchSupportFallback.Throw);

            Jeeves.TrenchSupport = ButlerBase.TrenchSupportFallback.DisableToolPromptSteering;
            Assert.IsTrue(Jeeves.TrenchSupport == ButlerBase.TrenchSupportFallback.DisableToolPromptSteering);

                Jeeves.AddTool(SteeringWheel);




            Assert.IsTrue(Jeeves.ExistsTool(SteeringWheel.ToolName));
        }
    }
}
