using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK.Tools
{
    public class ButlerTool_Network_HasAdapter : ButlerToolBase, IButlerToolAsyncResolver
    {
        public ButlerTool_Network_HasAdapter(IButlerVaultKeyCollection? KeyHandler) : base(KeyHandler)
        {
        }

        public override string ToolName => "CheckIfLiveNetworkConnection";

        public override string ToolDescription => "Check if caller has network connection";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return NoArgJson;
        }

        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).GetAwaiter().GetResult();
        }

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            Dictionary<string, string> Result = new();
            Result["HasValidNetworkConnecton"] = NetworkInterface.GetIsNetworkAvailable().ToString();
            return new  ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(Result));
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            return true;
        }
    }
}
