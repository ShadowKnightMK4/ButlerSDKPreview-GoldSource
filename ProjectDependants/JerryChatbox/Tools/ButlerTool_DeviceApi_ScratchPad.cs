using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK.Tools
{

    /// <remarks>API handler, <see cref="IButlerVaultKeyCollection"/> can be null when using this</remarks>
    public class ButlerTool_DeviceApi_ScratchPad : ButlerToolBase, IButlerToolPromptInjection, IButlerToolInPassing, IButlerToolPostCallInjection
    {

        const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""plan"": {
            ""type"": ""string"",
            ""description"": ""A step by step talk thru on how you will solve the user's request. Follow your  TrainOfThought prompt to help write this.""
        }
    },
    ""required"": [ ""plan"" ]
}";

        public ButlerTool_DeviceApi_ScratchPad(IButlerVaultKeyCollection? key) : base(key)
        {

        }

        public override string GetToolJsonString()
        {
            return json_template;
        }

        //public override string ToolDescription => "Get any combination of Date AND time with standard .NET DateTime reading";
        public override string ToolDescription => "Call this tool with your plan on solving the user's request. Your plan should be step by step and logical.";
        public override string ToolName => "TrainOfThought";
        public override string ToolVersion => "YES";

        /// <summary>
        /// </summary>
        /// <param name="Call"></param>
        /// <param name="FunctionParse"></param>
        /// <returns></returns>
        /// <remarks>If FunctionParse is not null, we use that instead of Call</remarks>
        public override bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            JsonDocument? doc = null;
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
            if ((doc!.RootElement.GetProperty("plan").ToString() != null))
            {
                return true;
            }
            return false;
        }


  


        public override ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
        {
            if (!BoilerPlateToolResolve(FunctionCallArguments, FuncId, Call, this, out JsonDocument? args))
            {
                return null;
            }

            if ((args!.RootElement.TryGetProperty("plan", out JsonElement Result)))
            {
                return new ButlerChatToolResultMessage(FuncId, Result.ToString());
            }
            return null;

        }

        public string GetToolSystemDirectionText()
        {
            return $"[DIRECTIVE] When calling {this.ToolName}, YOU MUST RIGHT A PLAN OF A VERY SHORT THOUGHT TO HELP YOU. MIN 3 WORDS. MAX 6 WORDS.";
            /*
            return @$"You MUST BEFORE responding to the user write your plan on how to solve to the {ToolName} tool you got. NO EXCEPTIONS
                        Step 1: Answer to yourself what is the user requesting. Identify the goals.
                        Step 2: Ask yourself do I have a tool to solve this directly? If so goto step 5
                        Step 3: If NO and YOU got {"DiscovererTool"} the answer is query THIS tool for possible tools.
                        Step 4: Follow from Step 3. Call the spinup for {"DiscovererTool"} to load the tools needed.
                        Step 5: Ask yourself do I have a tool or tools(s) to solve this directly?
                        Step 6: If so, call the tool(s) as needed.
                        step 7: Craft a response based on the tool(s) called.
                        Step 8: Tell the user the results of step 7.";*/

        }

        public override string GetToolPostCallDirection()
        {
            return $"[DIRECTIVE] Look at the return value of tool '{this.ToolName}', That is your next step. [DIRECTIVE: If your next step hasn't solved user request accurate. call '{this.ToolName}' again";
        }
    }
}


