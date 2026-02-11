using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.ToolSupport.DiscoverTool;
/* this is effectively a prototype */
#nullable disable
#pragma warning disable

namespace ButlerSDK.Tools
{
    [ButlerTool_DiscoverAttributes(true)]
    public class ButlerTool_Expirement_VisitUrl : ButlerToolBase
    {
        public ButlerTool_Expirement_VisitUrl(IButlerVaultKeyCollection key) : base(key) 
        {

        }
        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""URLS"": {
            ""type"": ""array"",
            ""items"": {
                ""type"": ""string""
            },
            ""description"": ""This is an array of URLS to load. ""
        },
        ""ACTION"": {
            ""type"": ""string"",
            ""description"":  ""USE 'visit' to goto the passed URLS and fetch raw text. USE 'bye' to close the browser. ""
        },
        ""WANT_IMAGES"": {
            ""type"": ""string"",
            ""description"": ""If equal to 'true', the images are also returned as base64 encoded. Otherwise no images are returned""
        }
    },
    ""required"":  [ ""URLS"", ""ACTION"" ]
}";

        IWebDriver? Browser;
        public override string ToolName => "SeleniumGetURLs";

        public override string ToolDescription => "Use the Chromium to load a set of urls, returning the raw text. ";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return json_template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc">doc to read from</param>
        /// <param name="entry">property to read from. Note assumes array with 'items' as the countaining property</param>
        /// <returns></returns>
        /// <remarks>Does not validate - that's <see cref="ValidateToolArgs(ChatToolCall, JsonDocument?)"/> task</remarks>
        protected static List<string> ExtractStringArray(JsonDocument doc, string entry)
        {
            var root = doc.RootElement.GetProperty(entry);
            List<string> ret = new();
            foreach (JsonElement item in root.EnumerateArray())
            {
                ret.Add(item.GetString());
            }
            return ret;
        }

        public override ButlerChatToolResultMessage? ResolveMyTool(string FunctionCallArguments, string FuncId, ButlerChatToolCallMessage? Call)
        {
            bool images_too = false;
            Dictionary<string, List<string>> ret = new();
            List<string> Urls;
            JsonDocument args;
            if (string.IsNullOrEmpty(FunctionCallArguments) && string.IsNullOrEmpty(FuncId) && (Call is null))
            {
                throw new ArgumentException("Pick either using FunctioncCallArgs as json string + func id, OR Call must NOT BE null");
            }
            if (Call is not null)
            {
                args = JsonDocument.Parse(Call.FunctionArguments);

                if (ValidateToolArgs(Call, args) == false)
                    return null;
            }
            else
            {
                args = JsonDocument.Parse(FunctionCallArguments);
                if (ValidateToolArgs(null, args) == false)
                {
                    return null;
                }
            }

            string test_bool = "false";
            try
            {
                test_bool = args.RootElement.GetProperty("WANT_IMAGES").GetString();
            }
            catch (KeyNotFoundException)
            {
                ; // fine
            }

            images_too = (test_bool == "true");
            if (Browser == null)
            {

                Browser = new ChromeDriver();
                
            }

            {
                // test for action.
                var act = args.RootElement.GetProperty("ACTION").GetString();
                switch (act)
                {
                    case "visit":
                        {
                            var urls = ExtractStringArray(args, "URLS");

                            foreach (string url in urls)
                            {
                                Browser.Navigate().GoToUrl(url);
                                ret[url] = new List<string>();
                                ret[url].Add((string)((IJavaScriptExecutor)Browser).ExecuteScript("return document.body.textContent"));
                                if (images_too)
                                {
                                    HttpClient img = new HttpClient();
                                    IList<IWebElement> images = Browser.FindElements(By.TagName("img"));
                                    foreach (IWebElement image in images)
                                    {
                                        string target = image.GetAttribute("src");
                                        try
                                        {
                                            ret[url].Add(img.GetStringAsync(target).Result);
                                        }
                                        catch (AggregateException e)
                                        {

                                        }
                                    }
                                        images_too = images_too;
                                }
                            }
                            break;
                        }
                    case "bye":
                        {
                            Browser.Dispose(); Browser = null;
                            return new ButlerChatToolResultMessage(FuncId, "Browser closed ok and the resources it used are free.");
                         //   break;
                        }
                }
            }

            StringBuilder ret_final = new();
            foreach (string key in ret.Keys)
            {
                for (int i = 0; i < ret[key].Count; i++)
                {
                    if (i == 0)
                    {
                        ret_final.Append($"URL:{key}, DATA: {ret[key][i]}");
                    }
                    else
                    {
                        ret_final.Append($"Image on URL: {key}: DATA {ret[key][i]}");
                    }
                }
            }
            return new ButlerChatToolResultMessage(FuncId, ret_final.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>
        /// <remarks>I'm impressed with the prompt to Bing Copilot to vicously check inputs for this method</remarks>
        public override bool ValidateToolArgs(ButlerChatToolCallMessage Call, JsonDocument? FunctionParse)
        {
            JsonElement root;
            if (FunctionParse != null)
            {
                root = FunctionParse.RootElement;
            }
            else
            {
                using (JsonDocument doc = JsonDocument.Parse(Call.FunctionArguments))
                {
                    root = doc.RootElement;
                }
            }

            // Validate the root element is an object
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // Validate the "URLS" property
            if (!root.TryGetProperty("URLS", out JsonElement urlsElement) || urlsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement url in urlsElement.EnumerateArray())
            {
                if (url.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
            }

            // Validate the "ACTION" property
            if (!root.TryGetProperty("ACTION", out JsonElement actionElement) || actionElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            string? action = actionElement.GetString();
            if (action is not null)
            {
                if (action != "visit" && action != "bye")
                {
                    return false;
                }
            }
            else
                return false;

            return true;
        }
    }
}
