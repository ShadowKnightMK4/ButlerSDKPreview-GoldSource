using ButlerSDK;
using ButlerSDK.ToolSupport;
using ButlerToolContract.DataTypes;
using ButlerSDK.ApiKeyMgr;
using ButlerSDK.Tools;

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

        string? apiKey = Environment.GetEnvironmentVariable("GEMINI_AI_KEY");
        if (apiKey is null)
            throw new InvalidOperationException("GEMINI_AI_KEY not set.");




        
        // create an Gemini powered butler.
        var butler = ButlerStarter.Instance.CreateGeminiButler(apiKey, "models/gemini-flash-latest");

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

        
        // give a push
        butler.AddUserMessage("What's the reason for gaming head phones and what 's the full date and time today.");

        // fire off the request to OpenAI's servers
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