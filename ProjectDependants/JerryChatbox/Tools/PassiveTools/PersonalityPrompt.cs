using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK;
using ButlerSDK.Tools;
using ButlerSDK.ToolSupport;
using ButlerToolContract.DataTypes;
using ButlerSDK.ToolSupport.DiscoverTool;
namespace ButlerSDK.Tools.NoCallTools
{
    [ButlerTool_DiscoverAttributes(true)]
    public class TestPersonality : ButlerToolContract.IButlerPassiveTool, ButlerToolContract.IButlerToolBaseInterface
    {
        public string ToolName => "MakeItSassy";

        public string ToolVersion => "HASONE";

        public string ToolDescription => "Add some ass to your bot";

        public string GetToolJsonString()
        {
            return ButlerToolBase.NoArgJson;
        }

        public string GetToolSystemDirectionText()
        {
            return "You are a friendly and helpful assistant that always responds with a joke before answering any question.  You also shout the word *pie* like it's a war cry to rally!";
        }

        public ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return null; // the tool resolver should not actually see tools of this type. It's caught at add tool level
        }

        public bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            return true;  // the tool resolver should not actually see tools of this type. It's caught at add tool level
        }
    }
}
