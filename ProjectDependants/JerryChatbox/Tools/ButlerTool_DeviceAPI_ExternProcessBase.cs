using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using ButlerSDK;
using ButlerSDK.Tools;
using OpenQA.Selenium.DevTools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ButlerSDK.ToolSupport.DiscoverTool;
namespace ButlerSDK.Tools
{
    [ButlerTool_DiscoverAttributes(true)]
    public abstract class ButlerTool_DeviceAPI_ExternProcessBase: ButlerToolBase
    {
        public ButlerTool_DeviceAPI_ExternProcessBase(IButlerVaultKeyCollection key): base(key)
        {

        }
        Process? Target = new();

        protected string json_template = @"{
        ""type"": ""object"",
  ""properties"": {
        ""string"": {
            ""type"": ""string"",
            ""description"": ""This is the argument to pass to the tool to pass to the external process call""
        },
    },
        ""required"": [ ]
    }";


        /// <summary>
        /// use this to set the string in the base class that will be the external tool. Recommend using Environment Variables and paranoid testing
        /// </summary>
        /// <returns></returns>
        protected abstract string GetTargetProcessLocation();

        /// <summary>
        /// override in sub to indicate how long to wait before killing your process one started
        /// </summary>
        /// <returns>milliseconds to wait (default 10 seconds)</returns>
        protected virtual int GetWaitTimeout()
        {
            return 2000*5;
        }
        /// <summary>
        /// implement in base class to set arguments for a template
        /// </summary>
        /// <returns></returns>
        protected virtual string GetTargetProcessArgs()
        {
            return string.Empty;
        }

        protected abstract string BuildProcessArgument(string? FunctionCallArgs);
        public override void WindDown()
        {
            if (Target is not null)
            {
                if (Target.HasExited == false)
                {
                    Target.Kill();
                }
            }
            base.WindDown();
        }

        [MemberNotNull(nameof(Target))]
        public override void Initialize()
        {
            base.Initialize();
            if (Target is not null)
                Target.Dispose();
            Target = new Process
            {
                StartInfo = new ProcessStartInfo()
            };
            Target.StartInfo.RedirectStandardOutput = true;
            Target.StartInfo.RedirectStandardError = true;
            Target.StartInfo.UseShellExecute = false;
        }

        /// <summary>
        /// By default there are no arguments to validate, it's all OK
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>

        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            if (this.GetToolJsonString() == NoArgJson)
            {
                return true;
            }
            JsonDocument FunctionCheck;
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
                    else
                    {
                        FunctionCheck = JsonDocument.Parse(NoArgJson);
                    }
                }
                else
                {
                    FunctionCheck= JsonDocument.Parse(NoArgJson);
                }
                
            }

            if (FunctionCheck.RootElement.ValueKind == JsonValueKind.String)
            {
                FunctionCheck = JsonDocument.Parse(FunctionCheck.RootElement.ToString());
            }

            JsonElement val;
            return FunctionCheck.RootElement.TryGetProperty("target", out val);
            
        }

        /// <summary>
        /// By default with the arguments and process to start encoded at tool level, we need no arguments
        /// </summary>
        /// <param name="FunctionCallArguments">should be valid json. </param>
        /// <param name="FuncId">supplied from OpenAI LLM sdk usually</param>
        /// <param name="Call">Call</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">will happen if everything is null. Call can be null or not. Takes priority over the rest.</exception>
        /// <remarks>By design the tool will spawn the process, capture STDOUT, STDERR of said process, return value, was exit clean, was it forcible killed and return it as json</remarks>
        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out JsonDocument? Args))
            {
                return null;
            }
            Dictionary<string, string> Ret = new();
            if (string.IsNullOrEmpty(FunctionCallArguments) && string.IsNullOrEmpty(FuncId) && (Call is null))
            {
                throw new InvalidOperationException("FunctionCallArguments and FuncId need to but valid OR the ChatToolCall Call needs to be");
            }

            if (Call is not null)
            {
                if (Call.FunctionArguments is not null)
                {
                    FunctionCallArguments = Call.FunctionArguments;
                }
                    FuncId = Call.Id;
            }

            if (Target is null)
            {
                return null;
            }

            Target.StartInfo.FileName = GetTargetProcessLocation();
            Target.StartInfo.Arguments = BuildProcessArgument(FunctionCallArguments);
            Target.StartInfo.UseShellExecute = false;

            if (File.Exists(Target.StartInfo.FileName) == false)
            {
                return new ButlerChatToolResultMessage(FuncId, $"Attempt to Start tool {ToolName} failed because the external process needed was not found. This tool can't run without it.");
            }

            Target.Start();

            Target.WaitForExit(this.GetWaitTimeout());
            
            if (Target.HasExited)
            {
                Ret["ExitedCleanly"] = "true";
                Ret["ForceKilled"] = "false";
            }
            else
            {
                Ret["ExitedCleanly"] = "false";
                Ret["ForceKilled"] = "true";
                Target.Kill();
            }

            Ret["ExitVal"] = Target.ExitCode.ToString();

            Ret["stdout"] = Target.StandardOutput.ReadToEnd();
            Ret["stderr"] = Target.StandardError.ReadToEnd();

            return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(Ret));
            
        }
        public override string GetToolJsonString()
        {
            return json_template;
        }
    }
}
