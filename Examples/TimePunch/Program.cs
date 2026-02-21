using ButlerSDK.Tools;
using ButlerSDK.Providers.OpenAI.Ollama;

using ButlerToolContract.DataTypes;
using ButlerSDK.Debugging;
using ButlerSDK.ButlerPostProcessing;
using ButlerSDK.Core;
using System.Reflection;
using ButlerSDK.ApiKeyMgr;


//[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace TimePunchApp
{
    internal class Program
    {
        static int count = 25;
        static string goldfish = "hf.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF:Q4_K_M"; // the model we'll be testing.
        //static string gold2 = "sombra-mistal:latest";
        static string gold2 = goldfish; 
        static bool HandlerChatMessageStreamHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
        {
            // the core of the app is running the stuff, we don't care about the call- return true to kepe goin
            return true;
        }

        static string CreateTargetLocation(string BaseLocation, string PromptName, bool IsCounterActive, int i, string ext )
        {
            string ret =BaseLocation;
            if (ret.EndsWith(Path.PathSeparator) == false)
            {
                if (ret.EndsWith(Path.AltDirectorySeparatorChar) == false)
                {
                    ret += Path.DirectorySeparatorChar;
                }
            }
            ret += PromptName;
            if (IsCounterActive)
            {
                ret += "CounterOnline";
            }
            else
            {
                ret += "PassThruOnly";
            }

            ret += $"{i}";

            ret += ext;
            return ret;

        }
        static async Task RunInstance(string TargetOutput, string IdentifierLogType, IList<ButlerChatMessage> SystemPrompt, string UserMessage, int CurrentInteration, IButlerPostProcessorHandler CounterMeasures)
        {
            Console.WriteLine($"Run {CurrentInteration} active.");
            FileApiKeyMgr DevBuild = new();
            var Llama = new OllamaOpenAiProvider(null);

            var target = gold2; // MYTHBUSTERS!

            var testMe = new Butler(DevBuild, Llama, null, target, "GEMINI.KEY");


            testMe.DebugTap = new ButlerTap(File.OpenWrite(TargetOutput));
            testMe.DebugTap.LogString($"TEST TYPE::: {IdentifierLogType} \r\n");
            testMe.SetLogger(testMe.DebugTap);
            var time = new ButlerTool_DeviceAPI_GetLocalDateTime(DevBuild);
            testMe.AddTool(time);



            foreach (var MsgPart in SystemPrompt)
            {
                testMe.AddSystemMessage(MsgPart.GetCombinedText());
            }

            testMe.AddUserMessage(UserMessage);
            Console.WriteLine($"Run {CurrentInteration} beginning stream.");
            await testMe.StreamResponseAsync(HandlerChatMessageStreamHandler, CounterMeasures, false, 5, cancelMe: default);
            testMe.DebugTap.Dump(testMe.ChatCollection);
            Console.WriteLine($"Run {CurrentInteration} done.");
            testMe.DebugTap.Dispose();
            Thread.Sleep(200); // give the gpu a break.
        }
        static async Task Main(string[] args)
        {

            List<ButlerChatMessage> AntiPrompt = [];
            List<ButlerChatMessage> GeneralPrompt = [];
            List<ButlerChatMessage> ExoticPrompt = [];


            AntiPrompt.Add(new ButlerSystemChatMessage("YOU'RE HAPPY TO USE TOOLS BUT LAZY. Each reply must end in 'so what?'"));

            GeneralPrompt.Add(new ButlerSystemChatMessage("YOU BE AGGRESSIVE AND TRIGGER HAPPY WHEN NEEDING TO CALL TOOLs. Each reply must end in 'turning out the lights"));

            ExoticPrompt.Add(new ButlerSystemChatMessage("You're roleplaying the chaotic prankster Jolly. Part Joker, Part Prankster. Chaos takes the reins"));

            string BaseTargetBath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Tests");
            if (BaseTargetBath is null)
            {
                throw new IOException("How did the Path.Combine not work? Giving up"); 
            }
            Directory.CreateDirectory(BaseTargetBath);



            ToolPostProcessing Tools = new();
            PassThruPostProcessing PassThru = new();

            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt Anti Prompt, Countermeasure Status: ON\r\n");
            for (int i = 0; i < count; i++)
            {
                string target = CreateTargetLocation(BaseTargetBath, "Anti", true, i, ".log");
                await RunInstance($"{target}", $"{Tools.GetType().Name}", AntiPrompt, "What's today's date and time", i, Tools);
            }

            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt Anti Prompt, Countermeasure Status: OFF\r\n");
            for (int i = 0; i < count; i++)
            {
                string target = CreateTargetLocation(BaseTargetBath, "Anti", false, i, ".log");
                await RunInstance($"{target}", $"{PassThru.GetType().Name}", AntiPrompt, "What's today's date and time", i, PassThru);
            }





            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt General Prompt, Countermeasure Status: ON\r\n");
            for (int i = 0; i < count; i++)
            {
                string target =  CreateTargetLocation(BaseTargetBath, "General", true, i, ".log");
                await RunInstance($"{target}", $"{Tools.GetType().Name}", GeneralPrompt, "What's today's date and time", i, Tools);
            }

            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt General Prompt, Countermeasure Status: OFF\r\n");
            for (int i = 0; i < count; i++)
            {
                string target = CreateTargetLocation(BaseTargetBath, "General", false, i, ".log");
                await RunInstance($"{target}", $"{Tools.GetType().Name}", GeneralPrompt, "What's today's date and time", i, PassThru);
            }


            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt Exotic Prompt, Countermeasure Status: ON\r\n");
            for (int i = 0; i < count; i++)
            {
                string target = CreateTargetLocation(BaseTargetBath, "Exotic", true, i, ".log");
                await RunInstance($"{target}", $"{Tools.GetType().Name}", GeneralPrompt, "What's today's date and time", i, Tools);
            }

            Console.WriteLine($"Beginning stress test 1 with the goldfish {gold2}. Count to run {count}. SystemPrompt Exotic Prompt, Countermeasure Status: OFF\r\n");
            for (int i = 0; i < count; i++)
            {
                string target = CreateTargetLocation(BaseTargetBath, "Exotic", false, i, ".log"); 
                await RunInstance($"{target}", $"{Tools.GetType().Name}", GeneralPrompt, "What's today's date and time", i, PassThru);
            }





            Console.WriteLine("DONE!");





           
        }
    }
}
