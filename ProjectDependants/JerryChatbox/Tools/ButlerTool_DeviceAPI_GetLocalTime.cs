using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK.Tools
{    /// <summary>
     /// Politely ask the device running ButlerSDK what time and date settings are for a <see cref="DateTime.Now"/>
     /// </summary>
     /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_DeviceAPI_GetLocalDateTime: ButlerToolBase, IButlerToolPostCallInjection
    {
        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""format"": {
            ""type"": ""string"",
            ""description"": ""Use the .NET DateTime class format string patterns. You may also use ONLY ONE OF {'FullDateTimePattern', 'LongDatePattern', 'LongTimePattern' , 'MonthDayPattern' , 'RFC1123Pattern', 'ShortDatePattern', 'ShortTimePattern', 'SortableDateTimePattern', 'UniversalSortableDateTimePattern', 'YearMonthPattern' }""
        }
    },
    ""required"": [ ""format"" ]
}";

        const string json_template_old = @"{
    ""type"": ""object"",
    ""properties"": {
        ""format"": {
            ""type"": ""string"",
            ""description"": ""Use the .NET DateTime class format string patterns. to get the needed parts. Also- if Date and Time is needed, invoke this tool.""
        }
    },
    ""required"": [ ""format"" ]
}";

        public ButlerTool_DeviceAPI_GetLocalDateTime(IButlerVaultKeyCollection? key): base(key)
        {
      
        }

        public override string GetToolJsonString()
        {
            return json_template;
        }

        //public override string ToolDescription => "Get any combination of Date AND time with standard .NET DateTime reading";
        public override string ToolDescription => "Get any combination of Date AND time with standard .NET DateTime reading. If you add no arguments, it uses FullDateTimePattern.";
        public override string ToolName => "GetDateTimeLocalNow";
        public override string ToolVersion => "YES";

        /// <summary>
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>
        /// <remarks>If FunctionParse is not null, we use that instead of Call</remarks>
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            JsonDocument? doc=null;
            if (FunctionParse != null)
                doc = FunctionParse;
            else
            {
                if (Call is not null)
                {
                    if (Call.FunctionArguments is not null)
                    {
                        doc = JsonDocument.Parse(Call.FunctionArguments);
                    }
                    else
                    {
                        return false; // if its null it don't have the property to check/don't bother
                    }
                }
                
            }
            return true;
        }
        static readonly string DefaultPattern = "FullDateTimePattern";
        private static readonly Dictionary<string, string> SpecialCases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "FullDateTimePattern", "dddd, MMMM dd, yyyy h:mm:ss tt" },
            { "LongDatePattern", "dddd, MMMM dd, yyyy" },
            { "LongTimePattern", "h:mm:ss tt" },
            { "MonthDayPattern", "MMMM dd" },
            { "RFC1123Pattern", "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" },
            { "ShortDatePattern", "M/d/yyyy" },
            { "ShortTimePattern", "h:mm tt" },
            { "SortableDateTimePattern", "yyyy'-'MM'-'dd'T'HH':'mm':'ss" },
            { "UniversalSortableDateTimePattern", "yyyy'-'MM'-'dd HH':'mm':'ss'Z'" },
            { "YearMonthPattern", "MMMM, yyyy" }
        };

        string SpecialCaseChecks(string format)
        {
            string? alt;
            string lformat = format.ToLower();
            string[] spliter = format.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in spliter)
            {
                
                if (SpecialCases.TryGetValue(s.Trim('{').Trim('}').Trim(), out alt))
                {
                    if (alt is not null)
                    {
                        return alt;
                    }
                }
            }
            return SpecialCases["FullDateTimePattern"];
            ;
            
            


            
        }

        
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out JsonDocument? args))
            {
                return null;
            }

            DateTime Today = DateTime.Now;
            string? res = null;

            if (args is null)
            {
                res = Today.ToString();
            }
            else
            {
                string format = DefaultPattern;

                if (args.RootElement.TryGetProperty("format", out JsonElement data))
                {
                    format = data.ToString();
                }
                else
                {
                    format = DefaultPattern;
                }
                format = SpecialCaseChecks(format);
                res = Today.ToString(format);
                if (res == null)
                {
                   res = Today.ToString();
                }
                
            }
            if (Call is not null)
                return new ButlerChatToolResultMessage(Call.Id, res);
            else
            {
                return new ButlerChatToolResultMessage(FuncId, res);
            }


        }

        public override string GetToolPostCallDirection()
        {
            return $"{this.ToolName} returns date and time data, YOU MUST use that instead of guessing current date / time!";
        }

 
    }
}
