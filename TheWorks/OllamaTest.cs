using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiKeyMgr;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.Core;
using ButlerSDK.Providers.OpenAI.Ollama;
using ButlerToolContract.DataTypes;
using SecureStringHelper;

namespace TheWorks
{
    internal static class OllamaTest
    {
        const string llama_drama = "NO_LLAMA_KEY";
        /// <summary>
        /// Run the test here
        /// </summary>
        public static void GladosMode()
        {
            Console.WriteLine("Starting Ollama test mode");

            var keys = new InMemoryApiKey();
            keys.AddKey(llama_drama, llama_drama);

            var try_crypt= keys.ResolveKey(llama_drama);
            if (try_crypt is null)
            {
                UnhappyPath.Fail($"Stored fake api key {llama_drama} in {keys.GetType().Name} did not succeed. Resolve Key Returned null", new InvalidDataException());
            }
            var detry = try_crypt!.DecryptString();
            if (detry != llama_drama)
            {
                UnhappyPath.Fail($"Stored fake api key {llama_drama} in {keys.GetType().Name} did not succeed. Resolve value Returned didn't match stored  value", new InvalidDataException());
            }
            Console.WriteLine($"Success in storing ${llama_drama} in {keys.GetType().Name} and later getting it back!");



            var Prov = new OllamaOpenAiProvider(null); // use defaultl local hard coded path
            Prov.Initialize();
            var support = Prov.SupportedModels;

            var target_model = string.Empty;
            if (support is not null)
            {
                Console.WriteLine("The provider supports model enumeration");
                foreach (string model in support.GetEnumerator())
                {
                    target_model = model;
                    Console.WriteLine($"Model:: {model}");
                }
            }
            else
            {
                Console.WriteLine("The provider does not support model enumeration");
                Console.WriteLine("Unable to continue with the works test");
            }

            {
                Butler Jarvis = new Butler(keys, Prov, Prov.DefaultOptions, target_model, llama_drama, null, null);
                Jarvis.AddSystemMessage("You are a poet.");
                Jarvis.AddSystemMessage("The current word 'pudding' is banned. Don't write poems with pudding in them");
                Jarvis.AddSystemMessage("If you can't write a poem - reply with a haiku of why");
                Jarvis.AddUserMessage("Please make a poem on chocolate pudding");

                var endresult = Jarvis.StreamResponse(null, false);

                foreach (ButlerChatMessage i in Jarvis.ChatCollection)
                {
                    Console.WriteLine(i.GetCombinedText());
                }

                Console.WriteLine("AI's reply was:");
                Console.WriteLine(Jarvis.ChatCollection.Last().GetCombinedText());
            }

        }
    }
}
