using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Core;
using ButlerSDK.Providers.OpenAI.DeepSeek;
using ButlerSDK.Tools;
using ButlerToolContract.DataTypes;

/*
 * Example of deep seek calling a tool and enumerating the model end point
 */
namespace DeepSeekExample
{
    internal class Program
    {
        /// <summary>
        /// Deep seek supports 2 models, display them via the ButlerSDK Enumerator
        /// </summary>
        /// <param name="plat"></param>
        static void ShowSupportedList(IButlerLLMProvider plat)
        {
            ArgumentNullException.ThrowIfNull(plat);

            var EnumStuff = plat.ChatCreationProvider.SupportedModels;

            if (EnumStuff is null)
            {
                Console.WriteLine("Unable to enumerate models");
            }
            else
            {
                Console.WriteLine("DeepSeek supports this list for the supplied key");
                int c = 0;
                foreach (string mod in  EnumStuff.GetEnumerator())
                {
                    c++;
                    Console.WriteLine($"Model {c}: Name {mod}");
                }
            }
        }


        /// <summary>
        /// set this to true to turn off simpler handler beflow
        /// </summary>
        static bool no_streaming = false;
        /// <summary>
        /// handler with a toggler turn off
        /// </summary>
        /// <param name="content">the packet thing update</param>
        /// <param name="msg">message list so far</param>
        /// <returns></returns>
        /// <remarks>Remember in the butler llm stream, this handler *freezes* the streaming until you return. true = keep going, false = exit</remarks>
        static bool SimpleHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
        {
            if (!no_streaming)
            {
                // here we just loop thru the content, 
                for (int i = 0; i < content.ContentUpdate.Count; i++)
                {
                    if (!string.IsNullOrEmpty(content.ContentUpdate[i].Text))
                        Console.Write(content.ContentUpdate[i].Text);
                }
            }
            return true;
        }

        /// <summary>
        /// Our stub that just goes ok wait for the task main
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            var task = Real(args);
            task.Wait();
            return task.Result;
        }
        /// <summary>
        /// The real main()
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task<int> Real(string[] args)
        {

            Program.no_streaming = true;
            // load the seep seek provider and memory key
            var Shim = new DeepSeekOpenAiProvider();

            var Keys = new InMemoryApiKey();

            // proper is call this to setup. Memory however does *not* care.
            Keys.InitVault(null!);

            // const so to not get types
            string calling_key = "DEEPSEEK";


            // holds either the loaded invormental variable or our file
            string? key_data = Environment.GetEnvironmentVariable(calling_key);

            // try load from env first. then a hard coded path

            if (key_data == null)
            {
                Console.Write($"Warning: '{calling_key}' env value not found....");
                if (File.Exists(@"T:\DeepSeek.key.txt"))
                {
                    Console.WriteLine(@"Attempting hardcoded fallback at T:\Deepseek.key.txt");
                    key_data = File.ReadLines(@$"T:\DeepSeek.key.txt").First();
                    Keys.AddKey(calling_key, key_data);
                    Console.WriteLine("Key is loaded ok");
                }
                else
                {
                    Console.WriteLine("Can't locate required key 'DEEPSEEK'");
                }
            }
            else
            {
                Keys.AddKey(calling_key, key_data);
                Console.WriteLine("Key is loaded ok");
            }

            Keys.Authenticate(System.Reflection.Assembly.GetExecutingAssembly()); // in memmory don't care either

            
            Shim.Initialize(Keys.ResolveKey(calling_key)!); // pinky promise it's not null. we just added it (unless someone is wrong?)


            ShowSupportedList(Shim);


            
            
            var ChatObject = new Butler(Keys, Shim, Shim.DefaultOptions, "deepseek-flash", "DEEPSEEK", null, null);

            ChatObject.MainOptions.ReasoningEffort = ButlerToolContract.DataTypes.ButlerThinkingEffortChoice.Medium;

            ChatObject.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime(null));

            ChatObject.AddSystemMessage("Play a friendly nerd");

            ChatObject.AddUserMessage("Hey do you got the time?");

            // don't forget to await for async stuff.

            var result = await ChatObject.StreamResponseAsync(SimpleHandler!);

            Console.WriteLine("\r\n\r\n");
            Console.BackgroundColor = ConsoleColor.Red;
            
            Console.WriteLine("\r\n\r\nAI TURN OVER. Displaying its output.");
            Console.BackgroundColor = ConsoleColor.Black;


            Console.WriteLine(ChatObject.ChatCollection.Last().GetCombinedText());

            Console.WriteLine("\r\n\r\n");
            Console.BackgroundColor = ConsoleColor.Red;

            Console.WriteLine("\r\n\r\n/AI TURN OVER. End of AI TURN OUTPUT its output.");

            Console.BackgroundColor = ConsoleColor.Black;

            Console.WriteLine("Believe it! Signing off the Deep Seek Example");

            return 0;
        }
    }
}
