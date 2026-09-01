using ButlerSDK.ApiKeyMgr.Contract;
using Azure.AI.OpenAI;
using Azure.Maps.Search;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
    /// The object's objective is keep APIKEY and call in memory short a time as possible that .net allows. 
    /// </summary>
  
    /// <summary>
    /// Using Azure Maps, get GPS cords from any combo of address. Note doesn't validate.
    /// </summary>
    /// <remarks>Requires the <see cref="IButlerVaultKeyCollection"/> to be non null and have a KEY under 'AZUREMAPS' that's an azure Maps key</remarks>
    public class ButlerTool_AzureApi_ResolveGPSAddress : ButlerToolBase, IButlerToolAsyncResolver
    {
        
        public ButlerTool_AzureApi_ResolveGPSAddress(IButlerVaultKeyCollection key):base(key)
        {
            if (key is null)
            {
                throw new ButlerSDK.ApiKeyNotFound(ApiKeyNotFound.CannedMessage(nameof(ButlerTool_AzureApi_ResolveGPSAddress)));
            }
        }

        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""address"": {
            ""type"": ""string"",
            ""description"": ""Provide an address somewhere in the world.""
        }
    },
    ""required"": [ ""address"" ]
}";
        public override string GetToolJsonString()
        {
            return json_template;
        }

        public override string ToolDescription => "Use this tool to GET GPS from an Address or Fragment";
        public override string ToolName => "GetGPSFromAddress";
        public override string ToolVersion => "YES";

        // there's two steps for the tool, first get public IP. next feed to Azure
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId!, Call).GetAwaiter().GetResult();

        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            JsonDocument doc;
            if ((Call is null) && (FunctionParse is null))
                return false;
            if (Call == null)
            {
                doc = FunctionParse!;
            }
            else
            {
                if (Call.FunctionArguments is null)
                {
                    doc = JsonDocument.Parse(NoArgJson);
                }
                else
                {
                    doc = JsonDocument.Parse(Call.FunctionArguments);
                }
            }

            bool pass ;
            try
            {
                string add = doc.RootElement.GetProperty("address").ToString();
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


            using (var AzureHelp = new AzureGPSHelper(this.Handler!))
            {
                // pinky promise this isn't null. Why ? Because base class assigns Handler. And constructor punts null handler in *our* constructor
                var result = AzureGPSHelper.AzureGPSLookUp_Results.Unknown;
                var callres = AzureHelp.ResolveAddressToGPS(args.RootElement.GetProperty("address").ToString(), true, out result);
                if (result is not AzureGPSHelper.AzureGPSLookUp_Results.Success)
                {
                    return new ButlerChatToolResultMessage(FuncId, $"There was an error getting GPS: {Enum.GetName(typeof(AzureGPSHelper.AzureGPSLookUp_Results), result)}");
                }
                else
                {
                    return new ButlerChatToolResultMessage(FuncId, callres!);
                }
            }


        }
    }
}
