using ApiKeyMgr;
using ButlerSDK.Core;
using ButlerSDK.Providers.UnitTesting.MockProvider;
using ButlerSDK.ToolSupport;
using ButlerSDK.ToolSupport.Bench;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestDataTypes;

namespace CoreUnitTests.CurrentTests
{
    [TestClass]
    public class ButlerDefaultToolBenchResolver_ExcpetionMode
    {

        [TestMethod]
        public void ValidateState_DefaultChatSession_DefaultTool_DefaultBench_UseNewAction()
        {
            InMemoryApiKey x = new();
            x.AddKey("NONE", "NONE");
            Assert.IsNotNull(x.ResolveKey("NONE"));
            var jarvis = new Butler(x, new MockProviderEntryPoint(), null, "DOA", "NONE", null, null);

            // we literal do not care to use the thing. just validate the 
            // default setting of the chat session to use the exception feature.

            ToolResolver? DefaultResolver = jarvis.GetPrivateField<ToolResolver>("ToolExec");
            Assert.IsNotNull(DefaultResolver, "Hey the internal structure of butler changed? point this test to rip the tool resolver object ");

            ButlerToolBench? DefaultBench = jarvis.GetPrivateField<ButlerToolBench>("ToolSet");
            Assert.IsNotNull(DefaultBench, "Interhal structure of the butler object changed? point this to the tool bench object of the default butler chat sesison handle");

            Assert.IsTrue(DefaultBench.ToolValidationAction == ToolValidationMode.AlwaysThrow);
        }

        [TestMethod]
        public void ValidateState_DefaultBench_LegacyMode()
        {
            ButlerToolBench x = new();
            Assert.IsTrue(x.ToolValidationAction == ToolValidationMode.Legacy);
        }
    }
}
