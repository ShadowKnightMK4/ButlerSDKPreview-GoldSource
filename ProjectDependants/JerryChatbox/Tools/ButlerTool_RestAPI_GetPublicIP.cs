using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerToolContract;

namespace ButlerSDK.Tools
{
    /// <summary>
    /// ask @"https://api.ipify.org/ what public IP is the device ButlerSDK is running in
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_RestAPI_GetPublicIP: ButlerToolBase, IButlerToolAsyncResolver
    {
        public ButlerTool_RestAPI_GetPublicIP(IButlerVaultKeyCollection key) : base(key)
        {

        }
        const string site_template = @"https://api.ipify.org/";
        /*const string json_template = @"{
        ""type"": ""object"",
        ""properties"": {
        },
        ""required"": [ ]
    }";*/
        readonly string json_template = NoArgJson;
    public override string ToolVersion => "YES";
        public override string ToolName => "GetUserDevicePublicIP";
        public override string ToolDescription => "Gets the user device's public IP via api.ipify.org. Can be used any where that info is needed.";
        
        

        /// <summary>
        /// the tool does not have args
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            return true;
        }

        public override ButlerChatToolResultMessage? ResolveMyTool(ButlerChatToolCallMessage Call)
        {
            return base.ResolveMyTool(Call);
        }
        
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).GetAwaiter().GetResult();    
        }
        public override string GetToolJsonString()
        {
            return json_template;
        }

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (Call is not null)
            {
                if (Call.FunctionArguments is not null)
                {
                    FunctionCallArguments = Call.FunctionArguments;
                }
                else
                {
                    FunctionCallArguments = NoArgJson;
                }
                FuncId = Call.Id;
            }
            if (FunctionCallArguments is null)
            {
                return null;
            }

            var json = JsonDocument.Parse(FunctionCallArguments);
            if (!ValidateToolArgs(Call, json))
                return null;
            else
            {

                var ret = await HttpClientStuff.ButlerToolHttpTransport.RequestPage(site_template);

                return new ButlerChatToolResultMessage(FuncId, ret.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
