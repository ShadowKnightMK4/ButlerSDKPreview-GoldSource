using ButlerSDK;
using ButlerToolContract.DataTypes;
using ButlerSDK.Providers;

internal class Program
{
    /// <summary>
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
            throw new InvalidOperationException("OPENAI_API_KEY not set.");


        

        // create an OpenAi powered butler
        var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt-4o");

        // set a system level prompt for the LLM
        butler.AddSystemMessage("You start each reply with 'what's up' but are helpful otherwise.");
        // give a push
        butler.AddUserMessage("What's the reason for gaming head phones?");

        // fire off the request to OpenAI's servers
        // var EndReason = await butler.StreamResponseAsync(null); // strictly speaking a handler is optional.

        var EndReason = await butler.StreamResponseAsync(SimpleHandler);

        // write the message to front.
        foreach (ButlerChatMessage msg in butler.ChatCollection)
        {
                Console.WriteLine(msg.GetCombinedText());
        }
        return 0;
    }
}