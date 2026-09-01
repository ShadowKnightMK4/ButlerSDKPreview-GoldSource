using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.ComponentModel;
using System.Net.Sockets;

namespace ButlerSDK.Tools
{
    public class ButlerToolNetworkDnsLookup : ButlerToolBase, IButlerToolAsyncResolver
    {
        /// <summary>
        /// This is how many IPs to return on a DNS lookup at most. Ment for ContextWindow Conserving
        /// </summary>
        public int MaxAddressToReturn = 5;
        public ButlerToolNetworkDnsLookup(IButlerVaultKeyCollection? KeyHandler) : base(KeyHandler)
        {
        }

        public override string ToolName => "DnsLookup";

        public override string ToolDescription => "Lookup the IP Address for DNS";

        public override string ToolVersion => "YES";

        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""target"": {
            ""type"": ""string"",
            ""description"": ""URL to look up""
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

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            JsonDocument result;
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out  result))
            {
                
                return null;
            }
            else
            {
                if (result!.RootElement.TryGetProperty("target", out JsonElement Target))
                {
                    int max = MaxAddressToReturn;
                    
                    Dictionary<string, string> ReturnData = new();

                    IPHostEntry? LookUp = null;
                    try
                    {
                        LookUp = await Dns.GetHostEntryAsync(Target.ToString());
                    }
                    catch (SocketException)
                    {
                        LookUp = null;
                    }

                    if (LookUp is not null) // yay host
                    {
                        ReturnData["HOSTEXISTS"] = "true";
                        ReturnData["HOSTNAME"] = LookUp.HostName;

                        if (max < 0)
                        {
                            max = Math.Abs(max);
                        }
                        if (max >= LookUp.AddressList.Length)
                        {
                            max = LookUp.AddressList.Length;
                        }
                        else
                        {
                            if (max == 0)
                            {
                                max = LookUp.AddressList.Length;
                            }
                        }

                        for (int i = 0; i < max; i++)
                        {
                            ReturnData[$"IP{i}"] = LookUp.AddressList[i].ToString();
                        }

                        max = Math.Min(Math.Abs(MaxAddressToReturn), Math.Abs(LookUp.Aliases.Length));
                        for (int i = 0; i < max; i++)
                        {
                            ReturnData[$"ALIAS{i}"] = LookUp.Aliases[i].ToString();
                        }
                    }
                    else
                    {
                        ReturnData["HOSTEXISTS"] = "false";
                    }





                        return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(ReturnData));
                }
                return null;
            }
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            JsonDocument? FunctionCheck=null;
            if (FunctionParse is not null)
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
            {
                return false;
            }
            if (FunctionCheck.RootElement.ValueKind == JsonValueKind.String)
            {
                FunctionCheck = JsonDocument.Parse(FunctionCheck.RootElement.ToString());
            }

            return FunctionCheck.RootElement.TryGetProperty("target", out JsonElement _);
        }
    }
}
