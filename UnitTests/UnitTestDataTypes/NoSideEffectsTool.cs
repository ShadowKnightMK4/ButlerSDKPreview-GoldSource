using ButlerSDK;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTests.UnitTestingTools
{
    [ToolSurfaceCapabilities(ToolSurfaceScope.MaxAvailablePermissions)]
    public class AllModeMockScope_AllAccessTagged: AllModeToolMockUnspecifiedScope
    {

    }
    [ToolSurfaceCapabilities(ToolSurfaceScope.NetworkRead)]
    public class AllModeMockScope_NetworkRead: AllModeToolMockUnspecifiedScope
    {

    }
    public class AllModeToolMockUnspecifiedScope : IButlerToolBaseInterface
    {
        public string ToolName => "WantAllPerms";

        public string ToolVersion => "YES";

        public string ToolDescription => "Test If add tools rejects higher than min scope more than input";

        public string GetToolJsonString()
        {
            return "{}";
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

    /// <summary>
    /// does nothing. Exposed ways to adjust what it returns for unit tests
    /// </summary>
    /// <remarks>api handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    [ToolSurfaceCapabilities(ToolSurfaceScope.NoPermissions)]
    public class NoSideEffectsTool : ButlerToolBase, IDisposable
    {


        public NoSideEffectsTool(IButlerVaultKeyCollection? key) : base(key)
        {

        }

        public override string GetToolJsonString()
        {
            return ButlerToolBase.NoArgJson;
        }

        /// <summary>
        /// What <see cref="ResolveMyTool(string, string?, ButlerChatToolCallMessage?)"/> returns
        /// </summary>
        public string ReturnedToolCallString = "The tool was called.";
        /// <summary>
        /// configure what the tool name is
        /// </summary>
        public string ReturnedToolName = "Stattracker";
        public override string ToolDescription => "Does Nothing but track its states for unit tests. Tests if protocol is called essentially";
        public override string ToolName => ReturnedToolName;
        public override string ToolVersion => "YES";

        public bool WasValidateCalled = false;
        public bool WasResolveCalled = false;
        public bool WasInitCalled = false;
        public bool WasWindDownCalled = false;

        public bool WasPlatformAPICalled = false;
        public bool WasDisposeCalled = false;


        /* 
            What this code below tests:

        
            Code base assumption for <ButlerToolKit>

            Tool is instanced ie  var Tool = new CoolTool();
            ButlerToolKit.AddTool(Tool)
                This routine called platform pass first and if it fails, rejects the tool
                This routine called init after platform pass works.


            Make sense?

            Now for testing and letting the unit test project provide itself:
                This do nothing class init() sets its first enum as last come wins the state for initialize and platform pass.
        
                If the code is indeed calling  Platform pass first..

                Platform pass has:
                    set its enum here to be platform pass.
                    returned true.

                Initialize called after platform pass
                    set its enum here shared with platform pass to be initialize
                    exited



            So our theory is if platform pass was called before initialize,
                    our dedicated platform pass flag is set, 
                    the enum will be initialize flag

                and if our initialize is called before platform pass
                    initialize dedicated flag is set,
                    enum will will platform pass.



            Bit twisted
                but it works
                    


            
         
        */
        enum FirstPassSetting
        {
            None = 0,
            PlatformPass,
            SpinUp
        }
        public bool WasPlatformFirst
        {
            get
            {
                switch (WasPlatform_Or_Init_First)
                {
                    case FirstPassSetting.None: return false;
                    case FirstPassSetting.PlatformPass: return false;
                    case FirstPassSetting.SpinUp: return true;
                }
                return false;
            }
        }
        public bool WasInitFirst
        {
            get
            {
                switch (WasPlatform_Or_Init_First)
                {
                    case FirstPassSetting.None: return false;
                    case FirstPassSetting.PlatformPass: return true;
                    case FirstPassSetting.SpinUp: return false;
                }
                return false;
            }
        }

        FirstPassSetting WasPlatform_Or_Init_First = FirstPassSetting.None;
        /// <summary>
        /// This bool sets what <see cref="ValidateToolArgs(ButlerChatToolCallMessage, JsonDocument?)"/> returns here.
        /// </summary>
        public bool ValidateToolReturnValue = true;

        /// <summary>
        /// reset teh bools we use to track what's called
        /// </summary>
        public void ResetTracker()
        {
            WasPlatform_Or_Init_First = FirstPassSetting.None;
           WasDisposeCalled =  WasValidateCalled = WasResolveCalled = WasInitCalled = WasWindDownCalled = WasPlatformAPICalled = false;
        }
        /// <summary>
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>
        /// <remarks>If FunctionParse is not null, we use that instead of Call</remarks>
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            WasValidateCalled = true;
            return ValidateToolReturnValue;
        }


        public override void Initialize()
        {
            base.Initialize();
            WasInitCalled = true;
            WasPlatform_Or_Init_First = FirstPassSetting.SpinUp;
        }

        public override bool CheckPlatformNeed(out string message)
        {
            WasPlatformAPICalled = true;
            WasPlatform_Or_Init_First = FirstPassSetting.PlatformPass;
            return base.CheckPlatformNeed(out message);
        }

        public override void WindDown()
        {
            base.WindDown();
            WasWindDownCalled = true;
        }
        public override ButlerChatToolResultMessage ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            WasResolveCalled = true;
            if (Call is not null)
                return new ButlerChatToolResultMessage(Call.Id, ReturnedToolCallString);
            else
            {
                return new ButlerChatToolResultMessage(FuncId, ReturnedToolCallString);
            }


        }

        public void Dispose()
        {
            WasDisposeCalled = true;
            GC.SuppressFinalize(this);
        }
    }

}
