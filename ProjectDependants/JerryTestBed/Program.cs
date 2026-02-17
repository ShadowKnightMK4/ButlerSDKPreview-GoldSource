using ButlerSDK.Tools;
using ButlerToolContract.DataTypes;
using ButlerSDK.Debugging;
using System.Diagnostics;
using ButlerSDK.ButlerPostProcessing;
using ButlerSDK;
using ButlerSDK.Providers.Gemini;
using OpenAiProvider;
using System.Text.Json;
using ButlerSDK.Core;

/*
 * A word. The TestBed project you see here is how I did manually testing aka edit and run.
 */
#pragma warning disable
namespace ButlerTestBed
{
    internal class Program
    {
        static bool NoneNullCall = false;
        //        static ToolResolver5? Schedule = null;
        static bool HandlerChatMessageStreamHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
        {


            foreach (var ContentPart in content.ContentUpdate)
            {

                if (ContentPart.Kind == ButlerChatMessagePartKind.Text)
                {
                    NoneNullCall = true;
                    Console.Write(ContentPart.Text);
                }
                else
                {
                    NoneNullCall = true;
                    Console.Write("<Image Removed>");
                }
            }
            return true;
        }
        static async Task Main(string[] args)
        {
            ButlerSDK.ApiKeyMgr.WindowsVault.WindowsVault  DevBuild = new();
            ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider OpenAi;
            //            var Llama = new Butler.Providers.LlamaProvider.Butler5ProviderLlama(new  DeepSeekV2PPr(), null);
            var Llama = new ButlerSDK.Providers.OpenAI.Ollama.OllamaOpenAiProvider(null);
            //OpenAi = new ButlerOpenAiProvider();
            var Gemini = new ButlerSDK.Providers.Gemini.ButlerGeminiProvider();
            DevBuild.Authenticate(System.Reflection.Assembly.GetExecutingAssembly());

            // swap this to the vault you want to use
            DevBuild.InitVault(@"C:\Users\Thoma\source\repos\ProjectJerry\ApiKeyVaultCreation\bin\Debug\net8.0-windows\Test.zip");



     
            /*
            testme.AddSystemChatMessage(@"You are a helpful llm. You do not need to tell
the user of your tool list, however if asked, it's ok. Do tell them if revealing your tool list,
that the tools may change depending on the chat as needed. 
               Your tool list will change thru the chat based on an external adaptive system. ");
            testme.AddSystemChatMessage("Assume all tools can potentially provide a part of the solution and invoke as needed. Do do not need to ask permission per tool.");
            //testme.AddSystemChatMessage(@"If you need to invoke a tool, examine the json in the tool that determines the arugments for the array ""required info"" and invoke tools as needed to get that info if input doesn't help""");
            testme.AddSystemChatMessage("Should you ");
            testme.AddSystemChatMessage("  For debug purposes Also walk me thru your actions at the end of your response place them after ## Action DEBUG.");
            */
            /*
            testme.AddSystemChatMessage(@"You are a helpful LLM. You do not need to disclose your tool list unless asked, and if asked, it's okay to reveal it. Mention that the tools may change during the chat as needed, based on an external adaptive system.");
            testme.AddSystemChatMessage("You also are free to attempt to answer with all tools at your disposal.");
            testme.AddSystemChatMessage("Consider all tools in your list as potential parts of the solution and invoke them as needed to help answer the user's query. If the risk of invoking a tool is low, you do not need to ask for permission—assume the user has granted it based on their message.");
            //testme.AddSystemChatMessage(@"If you need to invoke a tool, examine the JSON in the tool that determines the arguments for the array 'required info' and invoke tools as needed to get that info if input doesn't help.");
            testme.AddSystemChatMessage("If a tool involves any risk or danger, ask for the user's permission before invoking it. Clearly list the tools you intend to use. If the user grants permission, you may use any necessary tools to arrive at an answer.");
            testme.AddSystemChatMessage("If sometime involves the time and real time info, such as weather or holidays, assume the tool time clock is accurate and use that. Waive this assuption if working thru a known past event");
            testme.AddSystemChatMessage("For debug purposes, walk me through your actions at the end of your response and place them under '## Action DEBUG.'");

            //testme.AddSystemChatMessage("Additionally, if you add ## REMEMBER ## at the end of your response, you'll be be able to get it back by calling the tool. It's not displayed to the user and need not be human readable. ");
            //testme.AddSystemChatMessage("For debug purposes if asked about the ## REMEMBER ## freely talk about it.");
            //testme.AddTool(new ButlerTool_RestAPI_GetPublicHolidays());
            //testme.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime());
            testme.AddTool(new ButlerTool_RestAPI_GetPublicIP(DevBuild), 2);
            testme.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime(DevBuild), 4);
            testme.AddTool(new ButlerTool_AzureApi_GetCountryCode(DevBuild), 5);
            testme.AddTool(new ButlerTool_RestAPI_GetPublicHolidays(DevBuild), 6);
            testme.AddTool(new ButlerTool_Expirement_VisitUrl(DevBuild), 2000);
            testme.AddTool(new ButlerTool_AzureApi_ResolveGPSAddress(DevBuild), 7);
            

            testme.ParellelTools = true;
            testme.RateLimit = 4096;
            testme.DebugOptions.MaxTokens = 200;
            */
            //Butler3 testme = new Butler3(DevBuild);


            //var target = "sombra-mistal:latest";
            //var target = "hf.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF:Q4_K_M"; // MYTHBUSTERS!
            //var target = "smollm2:135m"; // MAYBE TOOL MALL
            //var target = "qwen2.5-coder:3b-instruct-q4_K_M"; //POSSIBLE brainstem. Unlikely
            // var target = "mistral:latest";  DOESN"T WORK
            //var target = "qwen3:4b"; DA BOOOT
            //var target = "butler-8k:latest"; DO BOOT 
            //var target = "llama3.1:latest"; da boot
            //var target = "gpt-oss:20b";  WORKS
            var target = "models/gemini-flash-latest"; //WORKS
            //var target = "gpt-4o"; //WORKS
            //var testme = new ButlerSDK.Butler(DevBuild, Llama, null, "models/gemini-flash-latest", "GEMINI.KEY");
            //var testme = new ButlerSDK.ButlerPostProcessing(DevBuild, Llama, null, target, "GEMINI.KEY", null, null); 

            var testme = new Butler(DevBuild, Gemini, null, target, "GEMINI.KEY", null, null);
            // testme.
            //var testme = new ButlerSDK.ButlerPostProcessing(DevBuild, OpenAi, null, target, "OPENAI.KEY");

            //var testme = new Butler5(DevBuild, Llama, null, "C:\\Users\\Thoma\\Downloads\\DeepSeek-Coder-V2-Lite-Instruct-Q8_0_L.gguf", null);
            //            testme.ChatModel = "gpt-4o-mini"; // TODO: Maybe let the provider offer a default mode? Cloud offers preset strings, local just supported files;


            testme.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime(DevBuild));
            testme.AddTool(new ButlerTool_RestAPI_GetPublicIP(DevBuild));
            testme.AddTool(new ButlerTool_DeviceAPI_ExternProcessNetworkAdaptor(DevBuild));
            testme.AddTool(new ButlerTool_DeviceAPI_ExternProcessIpConfig_FlushDNS(DevBuild));
            testme.AddTool(new ButlerTool_DeviceAPI_ExternProcessRenewDns(DevBuild));
            testme.AddTool(new ButlerTool_LocalFile_Load(DevBuild));
            testme.AddTool(new ButlerTool_AzureApi_GetCountryCode(DevBuild));
            testme.AddTool(new ButlerTool_RestAPI_GetPublicHolidays(DevBuild));
            testme.AddTool(new ButlerTool_DeviceAPI_ExternProcessPing(DevBuild));
            testme.AddTool(new ButlerTool_DeviceAPI_ExternProcessTraceRoute(DevBuild));
            //testme.AddTool(new ButlerTool_Expirement_VisitUrl(DevBuild));
            testme.AddTool(new ButlerTool_AzureApi_ResolveGPSAddress(DevBuild));
            //testme.AddTool(new ButlerTool_ExtendedChatWindow(DevBuild));
            

             
            testme.TheToolBox = new ButlerSDK.ToolSupport.DiscoverTool.ButlerTool_DiscoverTools(DevBuild);
            testme.TheToolBox.AddDefaultButlerSources(DevBuild);
            // this is just a way for it to can the running module (us) for tools
            testme.TheToolBox.AddDefaultButlerSources(DevBuild);
            testme.TheToolBox.AssignButler(testme);
            testme.AddTool(testme.TheToolBox);
            


            

            



            ButlerTap DebugMe = new ButlerTap(File.OpenWrite("butler_gemini_log.txt"));
            testme.DebugTap = DebugMe;






            
            testme.AddSystemMessage("You are an helpful ai and have a set of tools. Call them as needed to answer input.  Consider the user input as granting permission for you to call any tool to answer. Additionally, built your responses based off the ##personality## filter in the next message and the guiding of the user preferences.");
           
          





            {
                
                Console.WriteLine($"Powered with {testme.ChatModel}");
                ButlerChatFinishReason? StopAt = ButlerChatFinishReason.Stop;
                testme.MainOptions.ToolChoice = ButlerChatToolChoice.Auto;

                testme.MainOptions.AllowParallelToolCalls = true;
                
                int cs_index = 0;
                Task T;
                testme.SetLogger(DebugMe);
     
             
                while (true)
                {

                    T = testme.StreamResponseAsync(HandlerChatMessageStreamHandler, new ToolPostProcessing(), false, 5, cancelMe: default);
                    await T;
                    if (T.IsCompleted == false)
                    {
                        Debugger.Break();


                    }
                    else
                    {

                        if (NoneNullCall) Console.WriteLine();
                        NoneNullCall = false;
                        if (StopAt == ButlerChatFinishReason.Stop)
                        {
                            string? user = Console.ReadLine();
                            if (user is null)
                            {
                                Console.WriteLine();
                                Console.WriteLine();
                                Console.WriteLine();
                            }
                            else
                            {
                                if (user is "//")
                                {
                                    DebugMe.Dump(testme.ChatCollection);
                                    break;
                                }
                                testme.AddUserMessage(user);
                            }
                        }
                    }
                }
            }
        }
    }
}
