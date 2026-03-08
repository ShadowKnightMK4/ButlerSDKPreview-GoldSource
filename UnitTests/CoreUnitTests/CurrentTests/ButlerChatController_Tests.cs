using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.Core;
using ButlerSDK.Providers.UnitTesting.MockProvider;
using ButlerSDK.ToolSupport.Bench;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreUnitTests.CurrentTests
{
    [TestClass]
    public class ButlerChatController_Tests
    {
        [TestMethod]
        public void Butler_TestPassThruRecovery_TripsExecptionOnFail()
        {
            var dummyKeyMgr = new EnvironmentApiKeyMgr();
            var Dummy = new ErrorMockProviderEntryPoint();
            var TestMe = new Butler(dummyKeyMgr, Dummy, null, "None", Butler.NoApiKey, null, null);
            
            Assert.ThrowsExceptionAsync<MockException>(async () =>
            {
                await TestMe.StreamResponseAsync
                (null!, null, false, 5, 5, default); // the ! is because we want this to be null and go boom
            }
            );
        }
  
        [TestMethod]
        public void Butler_DefaultConstruction_AssignsDefaultToolBench_WhenNoneProvided()
        {
            var Dummy = new EnvironmentApiKeyMgr();
            var TestMe = new Butler(Dummy, new MockProviderEntryPoint(), null, "None", Butler.NoApiKey, null, null);

            // now we try reflict
            var field = typeof(ButlerBase).GetField("ToolSet", BindingFlags.Instance | BindingFlags.NonPublic);

            

            Assert.IsNotNull(field);
            var value = field.GetValue(TestMe);

            Assert.IsNotNull(value);
            Assert.IsInstanceOfType(value, typeof(IButlerToolBench)); 
            Assert.IsInstanceOfType(value, typeof(ButlerToolBench));
        }


    }
}
