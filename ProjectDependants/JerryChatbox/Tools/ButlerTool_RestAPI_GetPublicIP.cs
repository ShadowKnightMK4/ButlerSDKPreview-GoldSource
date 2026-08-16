using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerToolContract;
using System.Net;

namespace ButlerSDK.Tools
{
    /// <summary>
    /// ask @"https://api.ipify.org/ what public IP is the device ButlerSDK is running in
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_RestAPI_GetPublicIP: ButlerToolBase, IButlerToolAsyncResolver
    {
        private TimeSpan LagCounter = TimeSpan.FromMilliseconds(300);
        private HttpClient? Override = null;
        string[] AddressList =
        {
            "https://api64.ipify.org/",
            "https://api.ipify.org"
        };
        public ButlerTool_RestAPI_GetPublicIP(IButlerVaultKeyCollection key) : base(key)
        {

        }

        /// <summary>
        /// calling this in the class
        /// </summary>
        /// <param name="Override"></param>
        protected void ForceHttpClient(HttpClient Override)
        {
            this.Override = Override;
        }

        protected void SetTimeOut(TimeSpan x)
        {
            LagCounter = x;
        }
        const string site_template = @"https://api.ipify.org/";
        /*const string json_template = @"{
        ""type"": ""object"",
        ""properties"": {
        },
        ""required"": [ ]
    }";*/
        readonly string json_template = NoArgJson;
    public override string ToolVersion => "1.1.0";
        public override string ToolName => "GetUserDevicePublicIP";
        public override string ToolDescription => "Gets the user device's public IP via a RESTful request. Can be used any where that info is needed.";
        
        

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
            /* ship the below one not this code */
            // DEBUG CODE ONLY]
            // DO NOT UNCOMMENT THSI CODE =>  return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).GetAwaiter().GetResult();
            // the below is the one that won't red mark SyncCode_HostileSync_DontFreeze calavera unit test.
            return Task.Run(() =>
                        ResolveMyToolAsync(FunctionCallArguments, FuncId, Call))
                       .GetAwaiter()
                       .GetResult();
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
            HttpResponseMessage? ret = null;
            string? results;
            try
            {
                for (int i = 0; i < AddressList.Length; i++)
                {
                    try
                    {
                        if (Override != null)
                        {
                            ret = await Override.GetAsync(AddressList[i]).WaitAsync(LagCounter);
                        }
                        else
                        {
                            ret = await HttpClientStuff.ButlerToolHttpTransport.RequestPage(AddressList[i]).WaitAsync(LagCounter);
                        }
                        results = await ret.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromMilliseconds(200));
                        if (ret is not null)
                        {
                            if (ret.IsSuccessStatusCode)
                            {
                                if (!string.IsNullOrEmpty(results))
                                {
                                    /* ok the call went thru*/
                                    results = results.Trim();
                                    for (int j = 0; j < results.Length; j++)
                                    {
                                        if (char.IsWhiteSpace(results[j]))
                                        {
                                            results = results.Substring(0, j);
                                            break;
                                        }
                                    }
                                    if (IPAddress.TryParse(results, out var ActualIp))
                                    {
                                        return new ButlerChatToolResultMessage(FuncId, results);
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }
                                else
                                {
                                    // this will properate in the defualt setting upstream to go *hey something happened*
                                    return null;
                                }
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        if (ret != null)
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        if (ret != null)
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                    catch (UriFormatException)
                    {
                        if (ret != null)
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        if (ret != null)
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                    catch (HttpIOException)
                    {
                        if (ret != null)
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                    if (ret is not null)
                    {
                        if (ret.IsSuccessStatusCode)
                        {
                            break;
                        }
                        else
                        {
                            ret.Dispose();
                            ret = null;
                        }
                    }
                }
            }
            finally
            {
                if (ret is not null)
                {

                        ret.Dispose();

                }
            }
            return new ButlerChatToolResultMessage(FuncId, $"Error: Unable to connect to needed Uri to get IP.");
        }
    }
}
