using Azure.AI.OpenAI;
using ButlerToolContract;
using ButlerSDK.Tools;
using ButlerSDK.ToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnitTests.UnitTestingTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security;
using ButlerSDK.ToolSupport.Bench;


namespace UnitTests.CurrentTests
{
    [TestClass]
    public class ToolSecurityEnumTests
    {
        [TestMethod]
        public void Flags_Zero_Is_None()
        {
            Assert.AreEqual(0, (int)ToolSurfaceScope.NoPermissions);
        }

    }
    [TestClass]
    public class JerryToolKit_Tests
    {
        [TestMethod]
        public void CanCreateInstance_ShouldNotBeNull()
        {
            var TestMe = new ButlerToolBench();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(ButlerToolBench));
        }

        [TestMethod]
        public void ToolName_EnumCheck_TestsIfEnumerator_ToolNames_Works()
        {
            var TwoName = "Two";
            var OneName = "One";
            var TestMe = new ButlerToolBench();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(ButlerToolBench));
            var One = new NoSideEffectsTool(null);
            var Two = new NoSideEffectsTool(null);
            Assert.IsNotNull(One);
            Assert.IsNotNull(Two);
            Two.ReturnedToolName = TwoName;
            One.ReturnedToolName = OneName;

            Assert.IsTrue(string.Compare(Two.ToolName, TwoName) == 0);
            Assert.IsTrue(string.Compare(One.ToolName, OneName) == 0);

            TestMe.AddTool(One, true); TestMe.AddTool(Two , true);
            var TestList = new List<string>();
            Assert.IsTrue(TestList.Count == 0);
            foreach (var name in TestMe.ToolNames)
            {
                TestList.Add(name);
            }

            Assert.IsTrue(TestList.Count == 2);
            Assert.IsTrue(TestList.Contains(TwoName));
            Assert.IsTrue(TestList.Contains(OneName));


        }


        /// <summary>
        /// Common code for a set of add and remove tools
        /// </summary>
        /// <param name="tool">creates instance of this class. Sets tool to it</param>
        /// <param name="kit">creates instance of this class. Sets tool to it</param>
        /// <param name="AddToolAlready">by default false, set this to have the tool added before return to kit</param>

        static void Basic_AddTool_CommonStuff(out NoSideEffectsTool tool, out ButlerToolBench kit, bool AddToolAlready=false)
        {
            var TestTool = new UnitTests.UnitTestingTools.NoSideEffectsTool(null);
            Assert.IsNotNull(TestTool);
            Assert.IsInstanceOfType(TestTool, typeof(NoSideEffectsTool));

            var TestMe = new ButlerToolBench();
            Assert.IsNotNull(TestMe);
            Assert.IsInstanceOfType(TestMe, typeof(ButlerToolBench));
            Assert.IsTrue(TestMe.ToolCount == 0);
            tool = TestTool;
            kit = TestMe;
            if (AddToolAlready)
            {
                kit.AddTool(tool.ToolName, tool, true);
            }
        }

        [TestMethod]
        public void AddTool_RequestAllowedScope_ShouldNotThrow()
        {
            AllModeMockScope_NetworkRead Dummy = new AllModeMockScope_NetworkRead();
            ButlerToolBench TestMe = new();




        
                TestMe.AddTool(Dummy.ToolName, Dummy, true, ToolSurfaceScope.NetworkRead);
      

            Assert.IsTrue(TestMe.ToolCount == 1);

        }

        [TestMethod]
        public void AddTool_RejectsExcessPermission_RequestAllAccess_AllowedOnlyNetworkRead()
        {
            AllModeMockScope_AllAccessTagged Dummy = new AllModeMockScope_AllAccessTagged();
            ButlerToolBench TestMe = new();




            Assert.ThrowsException<SecurityException>(() =>
            {
                TestMe.AddTool(Dummy.ToolName, Dummy, true, ToolSurfaceScope.NetworkRead);
            });

            Assert.IsTrue(TestMe.ToolCount == 0);
        }

        [TestMethod]
        public void AddTool_DoesNotAllowScope_NoNetworkRead()
        {
            AllModeMockScope_AllAccessTagged Dummy = new AllModeMockScope_AllAccessTagged();
            ButlerToolBench TestMe = new();

            

            Assert.ThrowsException<SecurityException>(() =>
            { TestMe.AddTool(Dummy.ToolName, Dummy, true, ToolSurfaceScope.NetworkWrite);
            });

            Assert.IsTrue(TestMe.ToolCount == 0);
        }
        [TestMethod]
        public void AddTool_TreatsUnspecifiedToolsAsMaxRequestedScope()
        {
            AllModeToolMockUnspecifiedScope Dummy=new AllModeToolMockUnspecifiedScope();
            ButlerToolBench TestMe = new();

            
            Assert.ThrowsException<SecurityException>(() => { TestMe.AddTool(Dummy.ToolName, Dummy); });

            Assert.IsTrue(TestMe.ToolCount == 0);
        }
        [TestMethod]
        /// <summary>
        /// Test if a valid named tool is added and exists
        /// </summary>
        public void AddTool_ToolCountShouldBeOne()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestMe;
            
            Basic_AddTool_CommonStuff(out Dummy, out TestMe);
            TestMe.AddTool(Dummy.ToolName, Dummy);
            Assert.IsTrue(TestMe.ToolCount == 1);
        }

        [TestMethod]
        /// <summary>
        /// Tests if initialize is failed *after* platform check interface by the tool kit
        /// </summary>
        public void AddTool_WithValidTool_ShouldCallPlatformPassBeforeInitialize()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestMe;
            Basic_AddTool_CommonStuff(out Dummy, out TestMe);
            TestMe.AddTool(Dummy.ToolName, Dummy);
            Assert.IsTrue(Dummy.WasPlatformAPICalled);
            Assert.IsTrue(Dummy.WasInitCalled);
            Assert.IsTrue(Dummy.WasPlatformFirst);
            Assert.IsTrue(Dummy.WasInitFirst == false);
        }

        [TestMethod]
        /// <summary>
        /// Tests winddown called if cleanup allowed
        /// </summary>
        public void RemoveTool_CleanupAllowed_WasWinddown_Called()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestMe;
            Basic_AddTool_CommonStuff(out Dummy, out TestMe);
            TestMe.AddTool(Dummy.ToolName, Dummy);
            TestMe.RemoveTool(Dummy.ToolName, true);
            Assert.IsTrue(Dummy.WasWindDownCalled);
        }

        [TestMethod]
        /// <summary>
        /// Tests disposal called if cleanup allowed
        /// </summary>
        public void RemoveTool_CleanupAllowed_WasDispose_Called()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            testMe.AddTool(Dummy.ToolName, Dummy);
            testMe.RemoveTool(Dummy.ToolName, true, true);
            Assert.IsTrue(Dummy.WasDisposeCalled);
        }


        [TestMethod]
        /// <summary>
        /// Tests winddown called *not* called if cleanup disabled
        /// </summary>
        public void RemoveTool_CleanupDisabled_WasWinddown_skipped()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            testMe.AddTool(Dummy.ToolName, Dummy);

            testMe.RemoveTool(Dummy.ToolName, false);
            Assert.IsFalse(Dummy.WasWindDownCalled);
        }

        [TestMethod]
        /// <summary>
        /// Tests winddown called *not* called if cleanup disabled
        /// </summary>
        public void RemoveTool_CleanupDisabled_WasDisposal_skipped()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            testMe.AddTool(Dummy.ToolName, Dummy);
            testMe.RemoveTool(Dummy.ToolName, false);
            Assert.IsFalse(Dummy.WasDisposeCalled);
        }

        [TestMethod]
        public void Basic_AddAndRemoveTool_VerifyUpdateCount()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestMe;
            Basic_AddTool_CommonStuff(out Dummy, out TestMe,true);
            Assert.IsTrue(TestMe.ToolCount == 1);
            TestMe.RemoveTool(Dummy.ToolName, true);
            Assert.IsTrue(TestMe.ToolCount == 0);
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidToolNameException))]

        public void AddInvalidToolName_ValidateFlagTrue_ShouldThrowException()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            Assert.IsTrue(testMe.ToolCount == 0);
            Dummy.ReturnedToolName = " Tool name not valid #$213"; // randomizing
            testMe.AddTool(Dummy.ToolName, Dummy, true);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidToolNameException))]

        public void AddEmptyToolName_ValidateFlagTrue_ShouldThrowException()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            Assert.IsTrue(testMe.ToolCount == 0);
            Dummy.ReturnedToolName = string.Empty; // randomizing
            testMe.AddTool(Dummy.ToolName, Dummy, true);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]

        public void Basic_AddNullToolName_ValidateFlagTrue_ShouldThrowException()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestThis;
            Basic_AddTool_CommonStuff(out Dummy, out TestThis);
            Assert.IsTrue(TestThis.ToolCount == 0);
            Dummy.ReturnedToolName = null!;// we want to see if null go boom
            TestThis.AddTool(Dummy.ToolName, Dummy, true);
        }


        [TestMethod]

        public void Basic_AddInvalidToolName_ValidateFlagFalse()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench TestMe;
            Basic_AddTool_CommonStuff(out Dummy, out TestMe);
            Assert.IsTrue(TestMe.ToolCount == 0);
            Dummy.ReturnedToolName = " This tool name is not gonna get stuck in side your head ,$#d fa"; // randomizing
            TestMe.AddTool(Dummy.ToolName, Dummy, false);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]

        public void Basic_AddEmptyToolName_ValidateFlagFalse()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            Assert.IsTrue(testMe.ToolCount == 0);
            Dummy.ReturnedToolName = string.Empty; // randomizing
            testMe.AddTool(Dummy.ToolName, Dummy, false);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]

        public void AddNullToolName_ValidateFlagFalse_ShouldThrowArgumentNullException()
        {
            NoSideEffectsTool Dummy;
            ButlerToolBench testMe;
            Basic_AddTool_CommonStuff(out Dummy, out testMe);
            Assert.IsTrue(testMe.ToolCount == 0);
            Dummy.ReturnedToolName = null!;// null boom check
            testMe.AddTool(Dummy.ToolName, Dummy, false);
        }

        [TestMethod]
        public void MultiThreadGuardFlag_SetAndClear_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            TestMe.MultiThreadGuard = true;
            Assert.IsTrue(TestMe.MultiThreadGuard);
            TestMe.MultiThreadGuard = false;
            Assert.IsFalse(TestMe.MultiThreadGuard);
        }


        [TestMethod]
        public void ValidateToolName_GoodName_ThrowOnErrorNopeFlag_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool OkName = new NoSideEffectsTool(null);
            OkName.ReturnedToolName = "GetThis";

            Assert.IsTrue(TestMe.ValidateToolName(OkName, true));
        }

        [ExpectedException(typeof(InvalidToolNameException))]
        [TestMethod]
        public void ValidateToolName_GoodName_ThrowOnErrorYesFlag_ShouldThrowInvalidToolName()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool BadName = new NoSideEffectsTool(null);
            BadName.ReturnedToolName = "BAD NAME - This should not have spaces and - symbol";

            // technically the expected value of ValidateToolName below should be false if rejecting name, 
            Assert.IsTrue(TestMe.ValidateToolName(BadName, true));
        }


        [TestMethod]
        public void ExistsTool_ExistingToolCheck_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool Tool = new NoSideEffectsTool(null);
            TestMe.AddTool(Tool.ToolName, Tool, true);
            Assert.IsTrue(TestMe.ExistsTool(Tool.ToolName));
        }

        [TestMethod]
        public void ExistsTool_ToolRemovalCheck_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool Tool = new NoSideEffectsTool(null);
            TestMe.AddTool(Tool.ToolName, Tool, true);
            Assert.IsTrue(TestMe.ExistsTool(Tool.ToolName));
            TestMe.RemoveTool(Tool.ToolName, true);

            Assert.IsFalse(TestMe.ExistsTool(Tool.ToolName));
        }

        [TestMethod]
        public void GetTool_ToolNameAsString_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool Tool = new NoSideEffectsTool(null);
            TestMe.AddTool(Tool.ToolName, Tool, true);
            Assert.IsTrue(TestMe.ExistsTool(Tool.ToolName));

            Assert.AreSame(TestMe.GetTool(Tool.ToolName), Tool);    
        }


        [TestMethod]
        public void GetTool_NonExistentTool_ReturnsNull_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();


            Assert.AreSame(TestMe.GetTool("NOTOOLNAME"), null);
        }

        [TestMethod]
        public void UpdateTool_ShouldNotThrow()
        {
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool One = new NoSideEffectsTool(null);
            NoSideEffectsTool Two = new NoSideEffectsTool(null);
            Assert.IsTrue(TestMe.ToolCount == 0);

            TestMe.AddTool(One.ToolName, One, true);


            Assert.IsTrue(TestMe.ToolCount == 1);
            Assert.IsTrue(TestMe.ExistsTool(One.ToolName));


            // now we replace One with Two

            TestMe.UpdateTool(One.ToolName, Two);

            Assert.IsTrue(TestMe.ToolCount == 1);
            Assert.IsTrue(TestMe.ExistsTool(One.ToolName));
            Assert.AreNotSame(One, TestMe.GetTool(Two.ToolName));

            // secondary check.
            Two.ReturnedToolName = One.ReturnedToolName + "2";
            Assert.IsTrue(string.Compare(Two.ToolName, One.ToolName, true) != 0);
        }

        [TestMethod]
        public void CallTool_ShouldNotThrow()
        {
            JsonDocument x = JsonDocument.Parse("{}") ;
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool One = new NoSideEffectsTool(null);
            
            Assert.IsNotNull(TestMe);
            Assert.IsNotNull(One);

            TestMe.AddTool(One, ToolSurfaceScope.AllAccess, true);
            Assert.IsTrue(TestMe.ToolCount == 1);
            Assert.IsTrue(TestMe.ExistsTool(One.ToolName));

            One.ReturnedToolCallString = "Call Done";

            var result = TestMe.CallToolFunction(One.ToolName, "TEST", x.RootElement.ToString(), out bool ok);


            Assert.IsTrue(ok);
            Assert.IsNotNull(result); // we know the NoSideEffectsTool does not return ull.
            Assert.IsTrue(string.Equals("TEST", result.Id));
            
        }


        [TestMethod]
        public void ValidateToolGetsInvalidTool()
        {
            JsonDocument x = JsonDocument.Parse("{}");
            ButlerToolBench TestMe = new ButlerToolBench();
            NoSideEffectsTool One = new NoSideEffectsTool(null);
            One.ReturnedToolCallString = "NOT NULL";
            Assert.IsNotNull(TestMe);
            Assert.IsNotNull(One);

            TestMe.AddTool(One);
            Assert.IsTrue(TestMe.ToolCount == 1);
            Assert.IsTrue(TestMe.ExistsTool(One.ToolName));
            One.ValidateToolReturnValue = false; // this tells validate tool for this return value
            var result = TestMe.CallToolFunction(One.ToolName, "TEST", x.RootElement.ToString(), out bool ok);


            Assert.IsFalse(ok);
            Assert.IsTrue(string.Equals("TEST", result!.Id)); // No side effect tool never returns null
            Assert.IsTrue(string.Equals(One.ToolName, result.ToolName));
            Assert.IsTrue(One.WasValidateCalled);
            Assert.IsFalse(One.WasResolveCalled);

            
        }


    }
} 
