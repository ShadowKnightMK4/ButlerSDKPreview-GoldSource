using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using ButlerSDK.HttpClientStuff;
using Json.More;
using OpenAI.Chat;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerToolContract;

namespace ButlerSDK.Tools
{
    /// <summary>
    /// ask https://date.nager.at/api/v2/publicholidays for info based on year and country code
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_RestAPI_GetPublicHolidays: ButlerToolBase, IButlerToolAsyncResolver
    {
        const string site_template = @"https://date.nager.at/api/v2/publicholidays/{year}/{countrycode}";
        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""year"": {
            ""type"": ""number"",
            ""description"": ""This is the year to pass to the function call - example 2022, 2024, 2030""
        },
        ""country"": {
            ""type"": ""string"",
            ""description"": ""This should always be a 2 character country code recognized as the world country code for that country. US for United States,  ZA for South Africa, VA or Vatican City and so on.""
        }
    },
    ""required"": [ ""year"", ""country"" ]
}";


        /*
         * 
    ""year"": ""2022"",
    ""country"": ""US"",
    ""holidays"": [
        {
            ""date"": ""2022-01-01"",
            ""name"": ""New Year's Day"",
            ""localName"": ""New Year's Day"",
            ""fixed"": true,
            ""global"": true
        }
    ]*/

        public ButlerTool_RestAPI_GetPublicHolidays(IButlerVaultKeyCollection key):base(key)
        {

        }

        public override string GetToolJsonString()
        {
            return json_template;
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? Doc)
        {
            
            JsonDocument? FunctionCheck=null;
            if (Doc != null)
            {
                FunctionCheck = Doc.ToJsonDocument();
            }
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is null)
                    {
                        return false;// already failed validation
                    }
                    FunctionCheck = JsonDocument.Parse(Call.FunctionArguments);
                }
                else
                {
                    FunctionCheck = null;
                }
            }

            if (FunctionCheck is null)
                return false;

            if (FunctionCheck.RootElement.ValueKind == JsonValueKind.String)
            {
                FunctionCheck = JsonDocument.Parse(FunctionCheck.RootElement.ToString());
            }

        // remove when done.
        again:
            string year;
            string country;

            try
            {
                year = FunctionCheck.RootElement.GetProperty("year").ToString();
                country = FunctionCheck.RootElement.GetProperty("country").ToString();
            }
            catch (Exception)
            {
                goto again;
            }
            if (string.IsNullOrEmpty(year))
            {
                return false;
            }
            if (!int.TryParse(year, out _))
            {
                return false;
            }

            if (string.IsNullOrEmpty(country))
            {
                return false;
            }

            if (country.Length != 2)
                return false;

            return true;

        }
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
         {
            return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).GetAwaiter().GetResult();
        }

    

        public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            string year;
            string country;
            DateTime Expired = DateTime.MinValue;
            JsonDocument? doc;

            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out doc))
            {
                return null;
            }

            if (string.IsNullOrEmpty(FunctionCallArguments) && string.IsNullOrEmpty(FuncId) && (Call is null))
            {
                throw new InvalidOperationException("FunctionCallArguments and FuncId need to but valid OR the ChatToolCall Call needs to be");
            }

     
            if (!ValidateToolArgs(null, doc))
            {
                return null;
            }
            else
            {

                var root = doc.RootElement;
                year = root.GetProperty("year").ToString();
                country = root.GetProperty("country").ToString();
                if ((year == null) || (country == null))
                {
                    return null;
                }


            }



            string final_url = site_template.Replace("{year}", year);
            final_url = final_url.Replace("{countrycode}", country);

            {
                var combined_calls = ButlerToolHttpTransport.CreateCombinedCall();

                ButlerToolHttpTransport.AddCombinedCall(combined_calls, final_url);
                ButlerToolHttpTransport.CombinedCallsResolve(combined_calls).Wait();


                {
                    StringBuilder ret = new();
                    ret.Append($"This and the {combined_calls.Url.Count} tool messages are part of this call.");
                    foreach (var call in combined_calls.Url)
                    {
                        if (call.Value is not null)
                        {
                            string request;
                            request = await call.Value.Content.ReadAsStringAsync();
                            ret.Append(request);
                        }
                    }
                    return new ButlerChatToolResultMessage(FuncId, ret.ToString());
                }
            }
        }

        public override string ToolDescription => @"This tool makes an HTTP call to https://date.nager.at/api/v2/publicholidays/<YEAR>/<country> to load json describing USA dates that year";
        public override string ToolName => "GetUSAHolidayByYear";
        public override string ToolVersion => "YES";

    }
}
