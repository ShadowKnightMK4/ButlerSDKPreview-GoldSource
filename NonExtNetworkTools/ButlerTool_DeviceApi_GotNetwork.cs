using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.ToolSupport;
using ButlerToolContract;

namespace ButlerSDK.Tools
{
    [ToolSurfaceCapabilities(ToolSurfaceScope.NetworkWrite | ToolSurfaceScope.NetworkRead)]
    /// <summary>
    /// Test if the internet is up via DNS, network adapter and pinging a remote IP (8.8.8.8)
    /// </summary>
    public class ButlerTool_DeviceApi_GotNetwork : VirtualTool, IButlerToolAsyncResolver
    {
        public ButlerTool_DeviceApi_GotNetwork(IButlerVaultKeyCollection key) : base(key)
        {
            this._InnerTools.Add("DNSLOOKUP", new ButlerToolNetworkDnsLookup(key));
            this._InnerTools.Add("ADAPTER", new ButlerTool_Network_HasAdapter(key));
            this._InnerTools.Add("PING", new ButlerTool_Network_Ping(key));
        }

        readonly string json_template = ButlerToolBase.NoArgJson;
        public override string ToolVersion => "YES";
        public override string ToolName => "IsInternetConnectionAvailable";
        public override string ToolDescription => "Does a few checks to guess if internet is available.";



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


        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override string GetToolJsonString()
        {
            return json_template;
        }

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            bool ValidConnectionState = false;
            bool DnsCheckOk = false;
            bool Pingv4Results = false;
            bool Pingv6Results = false;
            // step one adapter
            var AdapterCheck = _InnerTools["ADAPTER"];
            ButlerChatToolResultMessage? AdapterResult;
            ButlerChatToolResultMessage? DnsResult;
            ButlerChatToolResultMessage? PingV4;
            ButlerChatToolResultMessage? PingV6;


            // step 1. DO we got any network? 
            if (AdapterCheck is IButlerToolAsyncResolver Async)
            {
                AdapterResult = await Async.ResolveMyToolAsync(null, FuncId, Call);
            }
            else
            {
                AdapterResult = AdapterCheck.ResolveMyTool(null, FuncId, Call);
            }

            JsonDocument NetworkConnectionJsonResult;

            if (AdapterResult is null)
            {
                return new ButlerChatToolResultMessage(FuncId,"Unable to Check Network at this time");
            }
            else
            {
                // see if we can rip a HasValidNetworkConnecton flag out of the thing
                if (AdapterResult is not null)
                {
                    NetworkConnectionJsonResult = JsonDocument.Parse(AdapterResult.GetCombinedText());

                    if (NetworkConnectionJsonResult.RootElement.TryGetProperty("HasValidNetworkConnecton", out JsonElement FlagMe))
                    {
                        if (FlagMe.ValueKind == JsonValueKind.String)
                        {
                            string? FlagCheck = FlagMe.GetString();
                            if (FlagCheck is null)
                            {
                                FlagCheck = string.Empty;
                            }
                            switch (FlagCheck.ToLowerInvariant())
                            {
                                case "true": ValidConnectionState = true; break;
                                case "false": ValidConnectionState = false; break;
                                default: throw new InvalidOperationException("Error intepreting underling tool");
                            }

                        }
                    }
                }
                else
                {
                    ValidConnectionState = false;
                }
            }


            JsonDocument DnsToolCallResult;
            if (ValidConnectionState)
            {
                var DnsCheck = _InnerTools["DNSLOOKUP"];
                Dictionary<string, string> DnsCheckArgs = new();
                DnsCheckArgs["target"] = "www.example.com";

                if (DnsCheck is IButlerToolAsyncResolver DnsAsync)
                {
                    DnsResult = await DnsAsync.ResolveMyToolAsync(JsonSerializer.Serialize(DnsCheckArgs), FuncId, Call);
                }
                else
                {
                    DnsResult = AdapterCheck.ResolveMyTool(JsonSerializer.Serialize(DnsCheckArgs), FuncId, Call);
                }
                if (DnsResult is not null)
                {
                    DnsToolCallResult = JsonDocument.Parse(DnsResult.GetCombinedText());

                    if (DnsToolCallResult.RootElement.TryGetProperty("HOSTEXISTS", out JsonElement HostExistsFlag))
                    {
                        if (HostExistsFlag.ValueKind == JsonValueKind.String)
                        {
                            string? hostflag = HostExistsFlag.GetString();
                            if (hostflag is not null)
                            {
                                hostflag = hostflag.ToLowerInvariant();
                                switch (hostflag)
                                {
                                    case "true": DnsCheckOk = true; break;
                                    case "false": DnsCheckOk = false;    break;
                                    default: throw new InvalidOperationException("Error intepreting underling tool");
                                }

                            }
                            else
                            {
                                DnsCheckOk = false;
                            }
                        }
                    }
                }
                else
                {
                    DnsCheckOk = false;
                }
            }

            JsonDocument? PingV4Call;
            JsonDocument? PingV6Call;

            if ((ValidConnectionState))
            {
                var PingTool = _InnerTools["PING"];
                Dictionary<string, string> IPv4Check = new();
                Dictionary<string, string> IPv6Check = new();
                IPv4Check["target"] = "8.8.8.8";
                IPv6Check["target"] = "2001:4860:4860:0:0:0:0:8888";

                if (PingTool is IButlerToolAsyncResolver PingAsync)
                {
                    PingV4 = await PingAsync.ResolveMyToolAsync(JsonSerializer.Serialize(IPv4Check), FuncId, Call);
                    PingV6 = await PingAsync.ResolveMyToolAsync(JsonSerializer.Serialize(IPv6Check), FuncId, Call);
                }
                else
                {
                    PingV4 = PingTool.ResolveMyTool(JsonSerializer.Serialize(IPv4Check), FuncId, Call);
                    PingV6 = PingTool.ResolveMyTool(JsonSerializer.Serialize(IPv6Check), FuncId, Call);
                }

                if (PingV4 is not null)
                {
                    PingV4Call = JsonDocument.Parse(PingV4.GetCombinedText());
                }
                else
                {
                    PingV4Call = null;
                }
                
                if (PingV6 is not null)
                {
                    PingV6Call = JsonDocument.Parse(PingV6.GetCombinedText());
                }
                else
                {
                    PingV6Call = null;
                }
                if (PingV4Call is not null)
                {
                    Pingv4Results = false;
                    if (PingV4Call.RootElement.TryGetProperty("Status", out JsonElement Status))
                    {
                        if (Status.ValueKind == JsonValueKind.String)
                        {
                            string? str = Status.GetString();
                            if (str is not null)
                            {
                                str = str.ToLowerInvariant();
                                if ((str == "0") || (str == "success"))
                                {
                                    Pingv4Results = true;
                                }
                                else
                                {
                                    Pingv4Results = false;
                                }
                                
                            }
                        }
                    }
                }
                else
                {
                    Pingv4Results = false;
                }

                if (PingV6Call is not null)
                {
                    Pingv6Results = false;
                    if (PingV6Call.RootElement.TryGetProperty("Status", out JsonElement Status))
                    {
                        if (Status.ValueKind == JsonValueKind.String)
                        {
                            string? str = Status.GetString();
                            if (str is not null)
                            {
                                str = str.ToLowerInvariant();
                                if ((str == "0") || (str == "success"))
                                {
                                    Pingv6Results = true;
                                }
                                else
                                {
                                    Pingv6Results = false;
                                }

                            }
                        }
                    }
                    
                }
                else
                {
                    Pingv6Results = false;
                }
            }


            // we've done our tests. pack that stuff up
            Dictionary<string, string> ret = new();
            ret["IPv4_Ping_Success"] = Pingv4Results.ToString();
            ret["IPv6_Ping_Success"] = Pingv6Results.ToString();
            ret["DnsLookup_Success"] = DnsCheckOk.ToString();
            ret["HasNetworkAdapter"] = ValidConnectionState.ToString();

            return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(ret));
        }
    }
}
