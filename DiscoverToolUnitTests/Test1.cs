using ApiKeyMgr;
using ButlerSDK.Tools;
using ButlerSDK.ToolSupport.DiscoverTool;
using ButlerToolContract;
using UnitTestDataTypes;
using UnitTests.UnitTestingTools;
namespace DiscoverToolUnitTests
{

    [TestClass]
    public sealed class ToolSurfaceDiscover_Tests
    {

        [TestMethod]
        public void DefaultResource_AttributeTest_DisableDiscover_False_FindsResource()
        {
            /* DiscoverToolWILLDISCOVER_TAGGED_FALSE is tagged with      [ButlerTool_DiscoverAttributes(false)]  Which means we WANT THIS TO BE DISCOVERED VIA reflection scanner*/
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.AllAccess);
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);

            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");
            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
            Assert.IsTrue(ToolsFound.Any(t => t.GetType() == typeof(DiscoverToolWILLDISCOVER_TAGGED_FALSE)));
        }

        [TestMethod]
        public void DefaultResource_AttributeTest_DisableDiscover_True_DOESNOT_FindResource()
        {
            /* DiscoverToolDoNotDiscover_TAGGED_TRUE is tagged with      [ButlerTool_DiscoverAttributes(True)]  Which means we WANT THIS TO NEVER DISCOVERED VIA reflection scanner*/
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.AllAccess);
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);

            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");

            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
            Assert.IsFalse(ToolsFound.Any(t => t.GetType() == typeof(DiscoverToolDoNotDiscover_TAGGED_TRUE)));
        }


        [TestMethod]
        public void DiscoverResource_DefaultResource_WorksWithNotVault()
        {
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.AllAccess);
            TestMe.Initialize(new InMemoryApiKey());
            Assert.IsNotNull(TestMe);

            // currently the same as null cause the default does *not* store the passed vault, it's just forwared to the found api tools
            Console.WriteLine("WARNING: if the default discover actually stores the vault or allows swapping, we'll need to update this test to check that the vault is stored or swappable, but for now this just checks that passing a vault doesn't break things");
        }



        [TestMethod]
        public void DiscoverResource_DefaultResource_WorksWithNullVault()
        {
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.AllAccess);
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);



        }

        [TestMethod]
        public void DiscoverResource_DefaultResource_RejectsNetworkReadTool_IfNotInSurface()
        {
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.SystemWrite); // long as it's not network
            // the mock class :AllModeMockScope_NetworkRead has - you guessed it - NetworkRead permissions, which should be rejected by the discoverer if it's properly checking permissions before discovering tools.
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);
            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");
            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
            Assert.IsFalse(ToolsFound.Any(t => t.GetType() == typeof(AllModeMockScope_NetworkRead)));

        }

        [TestMethod]
        public void DiscoverResource_DefaultResource_AllNetworkReadTool_IsInSurface()
        {
            var TestMe = new ToolSurfaceButlerTool_DiscovererResource(ButlerProtocolBase.ToolSecurity.ToolSurfaceScope.NetworkRead); // long as it's network
            // the mock class :AllModeMockScope_NetworkRead has - you guessed it - NetworkRead permissions, which should be rejected by the discoverer if it's properly checking permissions before discovering tools.
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);
            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");
            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
            Assert.IsTrue(ToolsFound.Any(t => t.GetType() == typeof(AllModeMockScope_NetworkRead)));

        }

    }
        [TestClass]
    public sealed class DefaultDiscoverResource_Tests
    {
        [TestMethod]
        public void DiscoverResource_DefaultResource_WorksWithNullVault()
        {
            var TestMe = new DefaultButlerTool_DiscoverResource();
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);
        }

        [TestMethod]
        public void DiscoverResource_DefaultResource_WorksWithNotVault()
        {
            var TestMe = new DefaultButlerTool_DiscoverResource();
            TestMe.Initialize(new InMemoryApiKey());
            Assert.IsNotNull(TestMe);
           
            // currently the same as null cause the default does *not* store the passed vault, it's just forwared to the found api tools
            Console.WriteLine("WARNING: if the default discover actually stores the vault or allows swapping, we'll need to update this test to check that the vault is stored or swappable, but for now this just checks that passing a vault doesn't break things");
        }


        [TestMethod]
        public void DefaultResource_AttributeTest_DisableDiscover_False_FindsResource()
        {
            /* DiscoverToolWILLDISCOVER_TAGGED_FALSE is tagged with      [ButlerTool_DiscoverAttributes(false)]  Which means we WANT THIS TO BE DISCOVERED VIA reflection scanner*/
            var TestMe = new DefaultButlerTool_DiscoverResource();
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);

            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");
            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
           Assert.IsTrue(ToolsFound.Any(t => t.GetType() == typeof(DiscoverToolWILLDISCOVER_TAGGED_FALSE)));
        }

        [TestMethod]
        public void DefaultResource_AttributeTest_DisableDiscover_True_DOESNOT_FindResource()
        {
            /* DiscoverToolDoNotDiscover_TAGGED_TRUE is tagged with      [ButlerTool_DiscoverAttributes(True)]  Which means we WANT THIS TO NEVER DISCOVERED VIA reflection scanner*/
            var TestMe = new DefaultButlerTool_DiscoverResource();
            TestMe.Initialize(null);
            Assert.IsNotNull(TestMe);

            var ToolsFound = TestMe.GetPrivateField<List<IButlerToolBaseInterface>>("ToolCollection");

            Assert.IsNotNull(ToolsFound); // in case the implementation changes and we forget to update the test, this will catch it.
            Assert.IsFalse(ToolsFound.Any(t => t.GetType() == typeof(DiscoverToolDoNotDiscover_TAGGED_TRUE)));
        }


    }
}
