using ButlerToolContract.DataTypes;
using ButlerSDK.ToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerToolContract;
using ButlerSDK.ToolSupport.Bench;
using ButlerProtocolBase.ToolSecurity;

namespace UnitTests.UnitTestingTools
{

    /// <summary>
    /// This mock is a mock tool box exposing stuff to let test configure what it does. Exposes 1 routine. And is ment for only plugging into eareas that need a toolbox but AREN'T seting <see cref="ToolResolver"/>
    /// </summary>
    public class MockToolBox : IButlerToolBench
    {
        /// <summary>
        /// Set to true to swap call id in returned massaget
        /// </summary>
        public bool SetCallId = true;
        /// <summary>
        /// call id set to this if set
        /// </summary>
        public string ReplacementCallId = string.Empty;
        /// <summary>
        /// Message contents of the returned call
        /// </summary>
        public string ResponseMessage = "IT worked";
        public bool Ok = true;

        public int ToolCount => throw new NotImplementedException();

        public IEnumerable<string> ToolNames => throw new NotImplementedException();

        public Dictionary<string, IButlerToolBaseInterface> fakeToolKit =new();
        /// <summary>
        /// For use in <see cref="ToolResolver"/> style test. Does NOT invoke anything. Will return a contracted 
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="CallId"></param>
        /// <param name="Arguments"></param>
        /// <param name="OK"></param>
        /// <returns></returns>
        public ButlerChatToolResultMessage? CallToolFunction(string FunctionName, string CallId, string Arguments, out bool OK)
        {
            OK = Ok;
            if (SetCallId) { CallId = ReplacementCallId; }

            return new ButlerChatToolResultMessage(CallId, ResponseMessage);
        }

        public ButlerChatToolResultMessage? CallToolFunction(IButlerToolBaseInterface Tool, string CallId, string Arguments, out bool OK)
        {
            OK = Ok;
            if (SetCallId) { CallId = ReplacementCallId; }

            return new ButlerChatToolResultMessage(CallId, ResponseMessage);
        }

        public  async Task<ButlerChatToolCallMessage?> CallToolFunctionAsync(IButlerToolBaseInterface targetTool, string CallId, string Arguments)
        {
            if (SetCallId) { CallId = ReplacementCallId; }

            return new ButlerChatToolResultMessage(CallId, ResponseMessage);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool ExistsTool(string name)
        {
            return fakeToolKit.TryGetValue(name, out var result);
        }

        public IButlerToolBaseInterface? GetTool(string name)
        {
            if (fakeToolKit.TryGetValue(name,out var result))
            {
                return result;
            }
            return null;
        }

        public void AddTool(string name, IButlerToolBaseInterface tool, ToolSurfaceScope Scope, bool ValidateNames)
        {

        }

        public void RemoveAllTools(bool AllowCleanup, bool DoNotRemoveSystemTools)
        {

        }

        public void RemoveTool(string name, bool AllowCleanup, bool DoNotRemoveSystemTools = true)
        {

        }
    };
}
