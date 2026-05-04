using ButlerProtocolBase.ToolSecurity;
using ButlerSDK.ToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestDataTypes;
using UnitTests.UnitTestingTools;
using ButlerToolContract;
namespace CoreUnitTests.CurrentTests
{

    [TestClass]
    public class ToolSurface_VirtualToolTests
    {
        [TestMethod]
        public void NestingTool_SecuritySweep_default()
        {
            NestingToolTool testme = new NestingToolTool(null, 255);
            Assert.IsNotNull(testme);
            Assert.IsTrue(NestingToolTool.NestedLevel(testme) == 256);

            ToolSurfaceFlagChecking.CheckMinRequirements(testme, ToolSurfaceScope.NoPermissions);
        }




        [TestMethod]
        public void NestingTool_SanityCheck_Mythbuster_WorstCase_test_intmax_NestingLevel_throws_not_supported()
        {
            Console.WriteLine("This tests for worst case theoritaly scenerio. of attempting Int.max levels deep is hard block - not supported");
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                NestingToolTool testme = new NestingToolTool(null, int.MaxValue);
                Assert.IsNotNull(testme);
            });
        }

        [TestMethod]
        public void NestingTool_SantityCheck_Mythbuster_SelfRefernece_NewLookUPDoesNotBreak_IsSafelyCaught()
        {
            /*
             * What we are testing here is that uis
             */
            NestingToolTool testme = new NestingToolTool(null, 1);
            Assert.IsNotNull(testme);
            var peek = testme.GetPrivateField<Dictionary<string, IButlerToolBaseInterface>>("_InnerTools");
            var peek_index = testme.GetPrivateField<string>("key");
            Assert.IsNotNull(peek);
            Assert.AreEqual(peek.Count, 1);
            Assert.IsNotNull(peek_index);
            Assert.IsTrue(peek.ContainsKey(peek_index));
            peek[peek_index] = testme;

            var result = ToolSurfaceFlagChecking.CheckMinRequirements(testme, ToolSurfaceScope.NoPermissions);


        }


        [TestMethod]
        public void NestingTool_SanityCheck_Mythbuster_WorstCase_test_2000_NestingLevel_throws()
        {

            NestingToolTool testme = new NestingToolTool(null, 2000);
            Assert.IsNotNull(testme);
            Assert.IsTrue(NestingToolTool.NestedLevel(testme) == 2001);

        }
        [TestMethod]
        public void NestingTool_SanityCheck_NestLevel_Matchings_Input()
        {

            NestingToolTool testme = new NestingToolTool(null, 255);
            Assert.IsNotNull(testme);
            Assert.IsTrue(NestingToolTool.NestedLevel(testme) == 256);


        }

        [TestMethod]
        public void NestingTool_SanityCheck_NestLevel_Matchings_Input_TestExMethod()
        {

            NestingToolTool testme = new NestingToolTool(null, 255);
            Assert.IsNotNull(testme);
            Assert.IsTrue(NestingToolTool.NestedLevel(testme) == 256);
            var walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();
            ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 2000, ref walker);
            Assert.IsTrue(HasAPermssion);
            // if you change the nesting tool to have something else
            if (Result != ToolSurfaceScope.StandardReading)
            {
                Assert.Fail("Unexpcted nesting tool permssion. Ensure the permssion tagged is ToolSurfaceScope.StandardReading OR update this unit test to the currently tagged version");
            }

        }


        [TestMethod]
        public void NestingTool_SanityCheck_NestLevel_Matchings_Input_TestExMethod_TriggersExceptionOnNo()
        {

            NestingToolTool testme = new NestingToolTool(null, 255);
            Assert.IsNotNull(testme);
            Assert.IsTrue(NestingToolTool.NestedLevel(testme) == 256);
            var walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();

            Assert.ThrowsException<VirtualTool_NestedOverflow>(() =>
            {
                ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 255, ref walker);
            });
            walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();

            Assert.ThrowsException<VirtualTool_NestedOverflow>(() =>
            {
                ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 1, ref walker);

            });
            walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();

            Assert.ThrowsException<VirtualTool_NestedOverflow>(() =>
            {
                ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 0, ref walker);
            });
            walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();

            {
                ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 256, ref walker);
            }

            walker = new HashSet<ButlerToolContract.IButlerToolBaseInterface>();
            {
                ToolSurfaceFlagChecking.LookupToolFlagEx(testme, out bool HasAPermssion, out ToolSurfaceScope Result, 0, 512, ref walker);
            }


        }
    }
}