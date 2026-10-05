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
                Console.Write(content.ContentUpdate[i].Text);
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



        // create an OpenAi powered butler
        var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "o1");

        // pick a pleasing reasoning effort. 
        // Note it is dependent on the provider and model.
        butler.MainOptions.ReasoningEffort = ButlerThinkingEffortChoice.Medium;


        // set a system level prompt for the LLM
        butler.AddSystemMessage("You start each reply with 'Hi-di-ho, neighbor' and answer like a friendly enthusiastic neighbor.");
        // give a push
        butler.AddUserMessage("What's an weird or unusual fact you know?");

        // fire off the request to OpenAI's servers
        // var EndReason = await butler.StreamResponseAsync(null); // strictly speaking a handler is optional.

        var EndReason = await butler.StreamResponseAsync(SimpleHandler);

        Console.WriteLine("AI TURN IS Over: Sending the final message below (there's a duplicate above this one)");
        // write the message to front.
        foreach (ButlerChatMessage msg in butler.ChatCollection)
        {
            Console.WriteLine(msg.GetCombinedText());
        }
        return 0;
    }
}