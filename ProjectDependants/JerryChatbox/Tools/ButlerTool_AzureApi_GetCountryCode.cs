using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.Tools.Internal;
using ButlerToolContract;
namespace ButlerSDK.Tools
{
    
    /// <summary>
    /// Using Azure Maps, we resolve a public IPv4 to a country code
    /// </summary>
    /// <remarks>Requires the <see cref="IButlerVaultKeyCollection"/> to be non null and have a KEY under 'AZUREMAPS' that's an azure Maps key</remarks>
    public class ButlerTool_AzureApi_GetCountryCode : ButlerToolBase, IButlerToolAsyncResolver
    {
        public ButlerTool_AzureApi_GetCountryCode(IButlerVaultKeyCollection key):base(key)
        {
            if (key is null)
            {
                throw new ButlerSDK.ApiKeyNotFound(ApiKeyNotFound.CannedMessage(nameof(ButlerTool_AzureApi_GetCountryCode)));
            }
        }
        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""IP"": {
            ""type"": ""string"",
            ""description"": ""This is an IPv4 Public Address""
        }
    }, 
    ""required"": [ ""IP"" ]
}";
        public override string GetToolJsonString()
        {
            return json_template;
        }

        public override string ToolDescription => "When given an IPv4 public address, gets the address's country code.";
        public override string ToolName => "GetCountryCodeFromIP";
        public override string ToolVersion => "YES";

        // there's two steps for the tool, first get public IP. next feed to Azure
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
                return ResolveMyToolAsync(FunctionCallArguments, FuncId!, Call).GetAwaiter().GetResult();
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            
            JsonDocument? doc;

            if  ( (Call is null) && (FunctionParse is null))
            {
                return false; 
            }
            if (Call == null)
            {
                doc = FunctionParse!; // shouldn't actually be null
            }
            else
            {
                if (Call.FunctionArguments is not null)
                {
                    doc = JsonDocument.Parse(Call.FunctionArguments);
                }
                else
                {
                    doc = null;
                }
            }
            if (doc is null)
                return false;

            bool pass = false;
            IPAddress tryme;
            try
            {
                string add = doc.RootElement.GetProperty("IP").ToString();
                tryme = IPAddress.Parse(add);
                pass = true;
            }
            catch (Exception)
            {
                pass = false;
            }
            return pass;
        }

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out JsonDocument? args))
            {
                return null;
            }

            if (args is null)
                return null;


#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var CountryCodeFetch = new AzureCountryCodeHelper(this.Handler!)) // reason for ! here is only constructor punts null on init. And it's a private variable
            {

                var ip = IPAddress.Parse(args.RootElement.GetProperty("IP").ToString());
                var result = CountryCodeFetch.ResolveIPToCountryCode(ip, true, out AzureCountryCodeHelper.AzureCountryCodeCall_Result res);
                if ((res != AzureCountryCodeHelper.AzureCountryCodeCall_Result.Success) || (result == null))
                {
                    return new ButlerChatToolResultMessage(FuncId, $"There was an error getting country code: {Enum.GetName(typeof(AzureCountryCodeHelper.AzureCountryCodeCall_Result), res)}");
                }
                else
                {
                    return new ButlerChatToolResultMessage(FuncId, result);
                }
            }
#pragma warning restore IDE0063 // Use simple 'using' statement

        }
    }
}
