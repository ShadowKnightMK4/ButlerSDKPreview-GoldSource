using ButlerLLMProviderPlatform.DataTypes;
using ButlerSDK;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.HttpClientStuff;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK.Tools
{
    /// <summary>
    /// ask https://date.nager.at/api/v2/publicholidays for info based on year and country code
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public abstract class ButlerTool_RestAPI_GetPublicHolidaysBase : ButlerToolBase, IButlerToolAsyncResolver
    {
        protected abstract string build_url(string year, string month);
        protected const string SiteTemplateBase = @"https://nagerholidays.com/api/v4/Holidays/{countrycode}/{year}/";
        protected readonly List<string> SiteTemplateTryArray = new List<string>();

 

    
        //      const string site_template = @"https://date.nager.at/api/v4/publicholidays/{countrycode}{year}/";
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

        public ButlerTool_RestAPI_GetPublicHolidaysBase(IButlerVaultKeyCollection key) : base(key)
        {
            SiteTemplateTryArray.Add(SiteTemplateBase);
        }

        public override string GetToolJsonString()
        {
            return json_template;
        }

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? Doc)
        {

            JsonDocument? FunctionCheck = null;

            
            if (Doc is not null)
            {
                FunctionCheck = Doc;
            }
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is null)
                    {
                        return false;// already failed validation
                    }
                    try
                    {
                        FunctionCheck = JsonDocument.Parse(Call.FunctionArguments);

                        if (FunctionCheck.RootElement.ValueKind == JsonValueKind.String)
                        {
                            FunctionCheck = JsonDocument.Parse(FunctionCheck.RootElement.ToString());
                        }
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
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



            if (!FunctionCheck.RootElement.TryGetProperty("year", out JsonElement Year))
            {
                return false;
            }
            if (!FunctionCheck.RootElement.TryGetProperty("country", out JsonElement country))
            {
                return false;
            }

            /* we acceept and run the math if it's a string, do we parse it as int? otherwise if it's an int, we try to parse it ok */
            if (Year.ValueKind != JsonValueKind.Number)
            {
                if (Year.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
                else
                {
                    if (!int.TryParse(Year.GetString(), out int Testval))
                    {
                        return false;
                    }
                   if (Testval < 1)
                    {
                        return false;
                    }
                }
            }
            else
            {
                var n = Year.GetInt32();
                if (n < 1)
                {
                    return false;
                }
            }
          
            






            if (string.IsNullOrEmpty(Year.ToString()))
            {
                return false;
            }
            if (!int.TryParse(Year.ToString(), out _))
            {
                return false;
            }

            if (string.IsNullOrEmpty(country.ToString()))
            {
                return false;
            }

            if (country.ToString().Length != 2)
                return false;

            return true;

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

                JsonElement YearStr;
                JsonElement CountryStr;
                string YEAR;
                string CC;
                
                var root = doc.RootElement;
                if (!root.TryGetProperty("year", out  YearStr))
                {
                    return new ButlerChatToolResultMessage(FuncId, $"Error: tool requests a year argument for the holidays to day");
                }
                else
                {
                    YEAR = YearStr.ToString();
                }

                if (!root.TryGetProperty("country", out CountryStr))
                {
                    return new ButlerChatToolResultMessage(FuncId, $"Error: tool requests a Country code argument for the holidays to day");
                }
                else
                {
                    CC = CountryStr.GetString()!;
                    if (CC is not null)
                    {
                        CC = CC.ToUpperInvariant();
                    }
                    else
                    {
                        return new ButlerChatToolResultMessage(FuncId, $"Error: tool requests a Country code argument  for the holidays to day that's a string");
                    }
                }




                if (CC.Length != 2)
                {
                    return null;
                }
                string? final_url = build_url(YEAR, CC);
                string results;
                HttpResponseMessage? ret = null;
                {
                    try
                    {
                        if (Override != null)
                        {
                            ret = await Override.GetAsync(final_url).WaitAsync(LagCounter);
                        }
                        else
                        {
                            ret = await HttpClientStuff.ButlerToolHttpTransport.RequestPage(final_url).WaitAsync(LagCounter);
                        }
                        if (ret.IsSuccessStatusCode)
                        {
                            results = await ret.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromMilliseconds(200));
                            if (ret is not null)
                            {
                                if (ret.IsSuccessStatusCode)
                                {
                                    if (!string.IsNullOrEmpty(results))
                                    {
                                        /* ok the call went thru*/
                                        results = results.Trim();
                                        return new ButlerChatToolResultMessage(FuncId, results);
                                    }
                                    else
                                    {
                                        // this will properate in the defualt setting upstream to go *hey something happened*
                                        return null;
                                    }
                                }
                                else
                                {
                                    return new ButlerChatToolResultMessage(FuncId, $"Connection OK. Failure response in getting holiday list. Http code {ret.StatusCode}");
                                }
                            }
                        }
                        else
                        {
                            return new ButlerChatToolResultMessage(FuncId, $"Error: Unable to get holiday list. Http reason {ret.StatusCode}");
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
                }
            }
            return null;





        }

        /// <summary>
        /// calling this in the class
        /// </summary>
        /// <param name="Override"></param>
        protected virtual void ForceHttpClient(HttpClient Override)
        {
            this.Override = Override;
        }

        protected virtual void SetTimeOut(TimeSpan x)
        {
            LagCounter = x;
        }

        HttpClient? Override = null;
        TimeSpan LagCounter = TimeSpan.FromMilliseconds(1000);
        public override string ToolDescription => @"This tool makes an HTTP call to https://date.nager.at/api/v2/publicholidays/<YEAR>/<country> to load json describing USA dates that year";
        public override string ToolName => "GetUSAHolidayByYear";
        public override string ToolVersion => "YES";

    }



    /// <summary>
    /// ask https://date.nager.at/api/v2/publicholidays for info based on year and country code
    /// </summary>
    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_RestAPI_GetPublicHolidays : ButlerTool_RestAPI_GetPublicHolidaysBase, IButlerToolAsyncResolver
    {

        public ButlerTool_RestAPI_GetPublicHolidays(IButlerVaultKeyCollection key) : base(key)
        {
        }

        public override string ToolDescription => @"This tool makes an HTTP call to https://date.nager.at/api/v2/publicholidays/<country>/<YEAR>/ to load json describing USA dates that year";
        public override string ToolName => "GetUSAHolidayByYear";
        public override string ToolVersion => "YES";

        protected override string build_url(string year, string code)
        {
            return SiteTemplateBase.Replace("{year}", year).Replace("{countrycode}",code);
        }
    }

}