using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using Google.Apis.CustomSearchAPI.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK.Tools
{
    public class ButlerTool_Network_Ping : ButlerToolBase, IButlerToolAsyncResolver
    {
        public ButlerTool_Network_Ping(IButlerVaultKeyCollection? KeyHandler) : base(KeyHandler)
        {
        }

        public override string ToolName => "Ping";

        public override string ToolDescription => "Run Ping on a target address";

        public override string ToolVersion => "YES";

        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""target"": {
            ""type"": ""string"",
            ""description"": ""THE IP or url to ping. ""
        }
    },
    ""required"": [ ""target"" ]
}";
        public override string GetToolJsonString()
        {
            return json_template;
        }

        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).GetAwaiter().GetResult();  
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            JsonDocument? FunctionCheck = null;
            if (FunctionParse != null)
            {
                FunctionCheck = FunctionParse;
            }
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is not null)
                    {
                        FunctionCheck = JsonDocument.Parse(Call.FunctionArguments);
                    }
                }
            }
            if (FunctionCheck is null)
                return false;

            if (FunctionCheck.RootElement.ValueKind == JsonValueKind.String)
            {
                FunctionCheck = JsonDocument.Parse(FunctionCheck.RootElement.ToString());
            }

            return FunctionCheck.RootElement.TryGetProperty("target", out JsonElement _);
        }

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out JsonDocument Args))
            {
                return null;
            }

            if (Args is not null)
            {
                if (Args.RootElement.TryGetProperty("target", out JsonElement targetJson))
                {
                    string Target = targetJson.ToString();
                    if (Target is null)
                    {
                        return null;
                    }
                    else
                    {
                        using (Ping NetworkTouch = new())
                        {
                            IPAddress IPTarget = IPAddress.Parse(Target); // let it go if this isn't a ip. 
                            try
                            {
                                var result = await NetworkTouch.SendPingAsync(IPTarget, 3000);
                                Dictionary<string, string> Retvalue = new();
                                Retvalue["RoundTrip"] = result.RoundtripTime.ToString();

                                string? EnumName = Enum.GetName(typeof(IPStatus), result.Status);
                                if (EnumName is not null)
                                    Retvalue["Status"] = EnumName;
                                else
                                    Retvalue["Status"] = "Unknown IPStatus";


                                    return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(Retvalue));
                            }
                            catch (PingException )
                            {
                                Dictionary<string, string> Retvalue = new();
                                Retvalue["RoundTrip"] = 0.ToString();

                                string? EnumName = Enum.GetName(typeof(IPStatus), IPStatus.Unknown);
                                if (EnumName is not null)
                                    Retvalue["Status"] = EnumName;
                                else
                                    Retvalue["Status"] = "Unknown IPStatus";

                                return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(Retvalue));
                            }
                        }
                    }
                }

            }
            return null;
        }
    }
}
