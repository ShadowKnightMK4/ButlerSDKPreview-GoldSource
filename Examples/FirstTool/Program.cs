using ButlerSDK;
using ButlerSDK.ToolSupport;
using ButlerToolContract.DataTypes;
using ButlerSDK.ApiKeyMgr;
using ButlerSDK.Tools;
using ButlerToolContract;
using System.Text.Json;

class SimpleMathTool : IButlerToolBaseInterface
{
    const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""term1"": {
            ""type"": ""number"",
            ""description"": ""Pass a fixed int number (5 for example), do not pass floats (5.3) for example""
        },
        ""term2"": {
            ""type"": ""number"",
            ""description"": ""Pass a fixed int number (5 for example), do not pass floats (5.3) for example""
        }
    },
    ""required"": [ ""term1"", ""term2"" ]
}";
    public string ToolName => "DoSimpleMath";

    public string ToolVersion => "YES";

    public string ToolDescription => "You can add 2 integers with cpu math instead of your training here.";

    public string GetToolJsonString()
    {
        return json_template;
    }

    /// <summary>
    /// If the tool does not use <see cref="IButlerToolAsyncResolver"/>, this resolver will be called. It's sync and will typically wait until done.
    /// </summary>
    /// <param name="FunctionCallArguments"></param>
    /// <param name="FuncId"></param>
    /// <param name="Call"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call)
    {
        // generally the workflow the system is use too is 
        // first very FunctionCallArguments and  Call + Call.FunctionArgs are all not null.
        // it's fine if one or few are.
        JsonDocument? args = null;
        if (FunctionCallArguments is null)
        {
            if (Call is null)
            {
                return new ButlerChatToolResultMessage(FuncId, "ERROR in calling this tool. Please add arguments");
            }
        }

        // try to snatch errors and GC the json args
        // NOTE: JsonDocument is diposable.
        try
        {
            if (Call is not null)
            {
                if (Call.FunctionArguments is not null)
                {
                    // first try the raw thing.
                    try
                    {
                        args = JsonDocument.Parse(Call.FunctionArguments);
                    }
                    catch (JsonException)
                    {
                        // ok json parser said no. and function args isn't null
                        if (FunctionCallArguments is not null)
                        {
                            try
                            {
                                // try the backup.
                                args = JsonDocument.Parse(FunctionCallArguments);
                            }
                            catch (JsonException)
                            {
                                // give up we can't see any arguments for call to run
                                return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON!)");
                            }
                        }

                    }
                }
            }
            else
            {
                // ok the Call (butlermessage) is null.
                // go directly to try the FunctionCallArgumetnts thing.
                if (FunctionCallArguments is null)
                {
                    // but that's null too!
                    // give up
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON!)");
                }
                try
                {
                    args = JsonDocument.Parse(FunctionCallArguments);
                }
                catch (JsonException)
                {
                    // ok that's not proper json
                    // give up
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }

                if (args is null)
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }
            }



            //after that mind field
            // the tool just rips 2 numbers 
            // as an llm is probability overflow
            // be sure to check terms and data
            // BEFORE running

            // for example the default runner by convention will always call validate tool first
            // but you'll typically see me have the resolver function call the tool too.
            string term1, term2;

            if (args != null)
            {
                // this battery of tests are
                // simply just, does term1 + term2 exist and are they numbers. That's it.


                if (!args.RootElement.TryGetProperty("term1", out JsonElement one))
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }
                if (!args.RootElement.TryGetProperty("term2", out JsonElement two))
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }


                if (one.ValueKind != JsonValueKind.Number)
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }

                if (two.ValueKind != JsonValueKind.Number)
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }

                if (!int.TryParse(one.ToString(), out int value1))
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }

                if (!int.TryParse(two.ToString(), out int value2))
                {
                    return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");
                }

                Dictionary<string, string> jsonret = new();
                // ok we add
                try
                {
                    int ret = value1 + value2;

                    // because llms might go what if null or a number. let's wrap it
                    
                    jsonret["result"] = ret.ToString();
                }
                catch (OverflowException)
                {
                    jsonret["result"] = "Number to big to do math with!";
                }

                return new ButlerChatToolResultMessage(FuncId, JsonSerializer.Serialize(jsonret));
            }
            return new ButlerChatToolResultMessage(FuncId, "Error in calling this tool. Ensure arguments are passed correctly (JSON syntax error!)");

        }
        finally
        {
            if (args is not null)
            {
                args.Dispose();
            }
        }
    }


    public bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
    {
        JsonDocument? x = null;
        try
        {
            if (Call != null) { 
               try
                {
                    if (Call.FunctionArguments is not null)
                    {
                        x = JsonDocument.Parse(Call.FunctionArguments);
                        if (x is null)
                        {
                            x = FunctionParse;
                        }
                    }
                    else
                    {
                        x = FunctionParse;
                    }
                }
                catch (ArgumentNullException)
                {

                }
                catch (JsonException)
                {

                }
                finally
                {
                    if (x is null)
                    {
                        x = FunctionParse;
                    }
                }
            }
            else
            {
                x = FunctionParse;
            }



            if (x is null)
            {
                return false;
            }
            else
            {
                string term1, term2;
                if (!x.RootElement.TryGetProperty("term1", out JsonElement one))
                {
                    return false;
                }
                if (!x.RootElement.TryGetProperty("term2", out JsonElement two))
                {
                    return false;
                }


                if (one.ValueKind != JsonValueKind.Number)
                {
                    return false;
                }

                if (two.ValueKind != JsonValueKind.Number)
                {
                    return false;
                }

                if (!int.TryParse(one.ToString(), out int value1))
                {
                    return false;
                }

                if (!int.TryParse(two.ToString(), out int value2))
                {
                    return false;
                }

                return true;
            }

        }
        finally
        {
            // x isn't null and x isn't the argmeent,
            // clean up
            if ((x is not null) && (!object.ReferenceEquals(x,FunctionParse)))
            {
                x.Dispose();
            }
        }
    }
}
static class Program
{    /// <summary>
     /// the handler delegate is used to expose a bit of the stream to dev (depending on the Counter Measures object (which is optional)
     /// </summary>
     /// <param name="content">the most recent packet</param>
     /// <param name="msg">the collection of message the stream is currently handling</param>
     /// <returns>You should return true to keep going</returns>
     /// <remarks>Keep in mind the stream is frozen until you continue</remarks>
    static bool SimpleHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
    {
        // here we just loop thru the content, 
        for (int i = 0; i < content.ContentUpdate.Count; i++)
        {
            if (!string.IsNullOrEmpty(content.ContentUpdate[i].Text))
                Console.WriteLine(content.ContentUpdate[i].Text);
        }
        return true;
    }
    static async Task<int> Main(string[] args)
    {

        /* Note the IButlerVaultKeyCollection exists to NOT HARD CODE KEYS. 
         * This is here to show easy usage with the facade 'starter' routine */

        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (apiKey is null)
        {
            if (File.Exists("T:\\openai.key.txt"))
            {
                apiKey = File.ReadLines("T:\\openai.key.txt").First();
            }
            else
            {
                throw new InvalidOperationException("OPENAI_API_KEY not set.");
            }
        }



        /// make the openai butler
        var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt4-o-mini"); 

        // set a System level prompt for the LLM
        butler.AddSystemMessage("You start each reply with 'what's up' but are helpful otherwise.");


        /* 
         * Let's add a tool.
         * There are 2 things to be aware of at this time.
         * 
         * Tools exist as C# classes that follow the Tool as defined in ButlerToolContracts.
         * 
         * Here for the example, know that Tools typically will take an instance of IButlerVaultKeyCollection.
         * 
         * However, IF YOU KNOW the tool does not need any access to the vault, passing null is fine. For example
         * 
         * If you're looking around In Intellisense, you may notice an AddSystemTool(). Those tools are
         * tools that can operate on the Butler object and its collection of tools itself.
         * For example the ButlerTool_Discoverer tool is a system tool, it modified the collection
         * of tools Butler sends to LLMs.
         */
        var ToolTime = new ButlerTool_DeviceAPI_GetLocalDateTime(null);

        // step for tools is add it to the core object
        butler.AddTool(ToolTime);


        // don't forget the custom tool.
        butler.AddTool(new SimpleMathTool());


        // give a push
        butler.AddUserMessage("What's the reason for gaming head phones and what 's the full date and time today. Also use the math tool to add 234+423");

        // fire off the request Gemini's servers
        var EndReason = await butler.StreamResponseAsync(SimpleHandler);

        // write the message to front.v
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine("PROCESS DONE: Showing the message data");
        Console.BackgroundColor = ConsoleColor.Black;
        foreach (var ChatEntry in butler.ChatCollection)
        {
            Console.WriteLine(ChatEntry.GetCombinedText());
        }
        return 0;
    }


}