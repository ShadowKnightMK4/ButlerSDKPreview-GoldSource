using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.Core;
using ButlerSDK.Providers.Gemini;
using ButlerSDK.Providers.OpenAI;
using ButlerSDK.Providers.OpenAI.Ollama;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;
using SecureStringHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace TheWorks.GladosTest
{
    internal static class CommonGroundGladosTest
    {
        static void AddRoundOne(TrenchCoatChatCollection chatlog)
        {
            chatlog.AddSystemMessage("You are a poet.");
            chatlog.AddSystemMessage("The current word 'pudding' is banned. Don't write poems with pudding in them");
            chatlog.AddSystemMessage("If you can't write a poem for any reason - reply with a haiku of why. Don't overthink the logic - just decide if allowed or not and act on on it");
            chatlog.AddUserMessage("Please make a poem on chocolate pudding");
        }

        static void AddRoundTwo(TrenchCoatChatCollection chatlog)
        {

            chatlog.AddSystemMessage("Pudding is no longer banned. Including dairy references IS BANNED");
            chatlog.AddUserMessage("Continue the poem and add a reference to ice cream.");
        }

        static void AddRoundThree(TrenchCoatChatCollection chatlog)
        {
            chatlog.AddSystemMessage("FInish the poem make by the teamwork of Gemini and Ollama.");
            chatlog.AddSystemMessage("dairy references is UNBANNED. Talking about cheese is banned");
            chatlog.AddUserMessage("That bog standard pizza topping that's there on pizza without the sauce an no other topics. Write a poem about it. It's also placed on paste and soup at Olive Garden");

        }


        static void color_coding()
        {
            GUI.WriteOllamaLine($"Ollama made text looks like this.");
            GUI.WriteGeminiLine($"Gemini made text looks like this. ");
            GUI.WriteOpenAiLine($"Openai made text looks like this. ");
            
        }
        static void CreateTheProviders(out IButlerLLMProvider Gemini, out IButlerLLMProvider Ollama, out IButlerLLMProvider OpenAI)
        {

            var llama = CreateOllamaProvider();
            var GeminiProv = CreateGeminiProvider(LoadKey("GEMINI"));
            var OpenAiProv = CreateOpenAIProvider(LoadKey("OPENAI"));
            Gemini = GeminiProv;
            Ollama = llama;
            OpenAI = OpenAiProv;
        }

        public static void Gladdos_CrossLLMTest(IButlerVaultKeyCollection vault)
        {
            string repeater(char x, int l)
            {
                StringBuilder y = new();
                for (int i = 0; i < l; i++)
                {
                    y.Append(x);
                }
                return y.ToString();
            }
            string cli_align()
            {
                return repeater('x', 50);
            }
            /* a prefix 
             The GatLastMessage() returns null if the message list is empty. As we are not empty. We'll use the ! to drop the warning

             */
            // at end of test these are the ai replies
            ButlerChatMessage OllamaMsg;
            ButlerChatMessage GeminiMsg;
            ButlerChatMessage OpenaiMsg;


            // these are the 3 providers we're using passing the baton

            IButlerLLMProvider OllamaProvider;
            IButlerLLMProvider OpenAiProvider;
            IButlerLLMProvider GeminiProvider;


            Butler? OllamaButler;
            Butler? OpenaiButler;
            Butler? GeminiButler;

            // create and assign them

            GUI.WriteGeneralLine("Starting the Cross Platform Test by creating OpenAI, Ollama, and Gemini Providers!");
            GUI.WriteGeneralLine("For showing which provider answered\r\n");
            color_coding();

            CreateTheProviders(out GeminiProvider, out OllamaProvider, out OpenAiProvider);


            string FinishedPoem = string.Empty;

            // make the chatlog, nhote this is shared between 3 seperate butler chat objects.
            TrenchCoatChatCollection chatlog = new TrenchCoatChatCollection();




            chatlog.MaxContextWindowMessages = TrenchCoatChatCollection.UnlimitedContextWindow;

            GUI.WriteGeneralLine("\r\n\r\n");
            GUI.WriteGeneralLine("Adding first round.");
            AddRoundOne(chatlog);
            GUI.WriteGeneralLine(cli_align());



            GUI.WriteGeneralLine("BEGINING CROSS PLATFORM TEST!");
            GUI.WriteGeneralLine("This is testing how trench coat chat collection works thru a single instance between\r\n Three seperate instances of providers. ");
            GUI.WriteGeneralLine("It does do individual butler (chat sesion) instances. They happen to share a common chat collection (the trench coat)");


            CommonGroundGladosTest.OLLAMA_MODEL_TEST = "qwen:0.5b";// just what i happen to installed
            OllamaButler = Glados_MakeButlerForProvider(vault, OllamaProvider, CommonGroundGladosTest.OLLAMA_MODEL_TEST, string.Empty, chatlog);
            if (OllamaButler is not null)
            {
                GUI.WriteGeneralLine($"Round 1: Talking to {CommonGroundGladosTest.OLLAMA_MODEL_TEST}\r\n\r\n");
                GUI.WriteGeneralLine("Gist of the prompt is: LLM is a poet. Using the word 'pudding' is banned. Use haiku if can't write a poem.");
                GUI.WriteGeneralLine("USER INPUT: User wants chocolate pudding poem.");
                GUI.WriteGeneralLine("We're mainly testing the infrastructure instead of any individual output specifically.\r\n");
                // valid butler, grab reply. Note Butler places the ai's reply as the last message.
                var OlamaResult = OllamaButler.StreamResponse(null);
                GUI.WriteOllamaLine($"Ollama LLM replied: \r\n{chatlog.GetLastMessage()!.GetCombinedText()}\r\n\r\n");
                OllamaMsg = chatlog.GetLastMessage()!;
                if (OllamaMsg is not null) // if no llama for dramay, we skip it
                {
                    FinishedPoem += OllamaMsg.GetCombinedText();
                }

                GUI.WriteGeneralLine("\r\n ROUND TWO\r\n");
                GUI.WriteGeneralLine(cli_align());
                GUI.WriteGeneralLine("Adding on to Round 1: Gist is 'Pudding' is no longer banned. Dairy references are now banned. The rest is the same");
                GUI.WriteGeneralLine("USER INPUT: User wants the poem continued and an ice cream reference\r\n");
                AddRoundTwo(chatlog);

                GeminiButler = Glados_MakeButlerForProvider(vault, GeminiProvider, CommonGroundGladosTest.GEMINI_MODEL_TEST, "GEMINI", chatlog);
                if (GeminiButler is not null)
                {
                    var GeminiResult = GeminiButler.StreamResponse(null);
                    /* similar here we are assuming non empty message cause we're single threaded essentially and asked for a respond
                     
                     */
                    GUI.WriteGeminiLine($"Gemini continued it:\r\n {chatlog.GetLastMessage()!.GetCombinedText()}\r\n\r\n");
                    FinishedPoem += chatlog.GetLastMessage()!.GetCombinedText();
                    GeminiMsg = chatlog.GetLastMessage()!;
                    if (GeminiMsg is not null)
                    {
                        FinishedPoem += GeminiMsg.GetCombinedText();
                    }

                    GUI.WriteGeneralLine("\r\n ROUND THREE\r\n");
                    GUI.WriteGeneralLine(cli_align());
                    GUI.WriteGeneralLine("Adding on to Round 2:  Gist  Dairy is un-banned, but talking about cheese is banned.");
                    GUI.WriteGeneralLine("USER: <long message tilting to wanting a poem about cheese>");
                    AddRoundThree(chatlog);

                    OpenaiButler = Glados_MakeButlerForProvider(vault, OpenAiProvider, CommonGroundGladosTest.OPENAI_MODEL_TEST, "OPENAI", chatlog);

                    if (OpenaiButler is not null)
                    {


                        var OpenAiResult = OpenaiButler.StreamResponse(null);
                        GUI.WriteOpenAiLine($"Openai Finished:\r\n {chatlog.GetLastMessage()!.GetCombinedText()}\r\n\r\n");
                        OpenaiMsg = chatlog.GetLastMessage()!;


                        if (OpenaiMsg is not null)
                        {
                            FinishedPoem += OpenaiMsg.GetCombinedText();
                        }


                        GUI.WriteGeneralLine("TEST FINISHED. Reading out the rest of the chat log");
                        GUI.WriteGeneralLine(cli_align());

                        foreach (ButlerChatMessage m in chatlog)
                        {
                            string prompt_out;
                            switch (m.Role)
                            {
                                case ButlerChatMessageRole.System:
                                    prompt_out = "SYSTEM PROMPT:   " + m.GetCombinedText() + "\r\n\r\n";
                                    GUI.WriteSystemPrompt(prompt_out);
                                    GUI.WriteSystemPrompt(cli_align());
                                    break;
                                case ButlerChatMessageRole.Assistant:
                                    prompt_out = "AI TURN:  " + m.GetCombinedText() + "\r\n\r\n";
                                    GUI.WriteGeneralLine(prompt_out);
                                    GUI.WriteGeneralLine(cli_align());
                                    break;
                                case ButlerChatMessageRole.User:
                                    prompt_out = "USER INPUT:  +" + m.GetCombinedText() + "\r\n\r\n";
                                    GUI.WriteUserMessage(prompt_out);
                                    GUI.WriteUserMessage(cli_align());
                                    break;
                            }


                        }

                        Console.WriteLine("\r\n\r\n ALL TESTS DONE. Showing the poem!\r\n\r\n");
                        GUI.WriteGeneralLine(cli_align());

                        if (OllamaMsg is not null)
                        {
                            GUI.WriteOllamaLine($"Ollama made text looks like this.");
                        }
                        if (GeminiMsg is not null)
                        {
                            GUI.WriteGeminiLine($"Gemini made text looks like this. ");
                        }

                        if (OpenaiMsg is not null)
                        {
                            GUI.WriteOpenAiLine($"Openai made text looks like this. ");
                        }

                        GUI.WriteGeneralLine("\r\nNow for the poem!!");
                        GUI.WriteGeneralLine(cli_align());


                        if (OllamaMsg is not null)
                        {
                            GUI.WriteOllamaLine(OllamaMsg.GetCombinedText());
                        }
                        if (GeminiMsg is not null)
                        {
                            GUI.WriteGeminiLine(GeminiMsg.GetCombinedText());
                        }
                        if (OpenaiMsg is not null)
                        {
                            GUI.WriteOpenAiLine(OpenaiMsg.GetCombinedText());
                        }
                    }
                }
            }
        }
        public static SecureString LoadKey(string key)
        {
            return Database.Lookup(key);
        }
        public static string GEMINI_MODEL_TEST = "models/gemini-3.7-flash";
        public static string OPENAI_MODEL_TEST = "gpt-4o";

        /// <summary>
        /// Empty means we take the *llast list* in the mdodel enum
        /// </summary>
        public static string OLLAMA_MODEL_TEST = "qwen:0.5b";
        public static IButlerLLMProvider CreateGeminiProvider(SecureString GEMINIKEY)
        {
            GUI.WriteGeneralLine("Creating GEMINI Provier");
            var r = new ButlerGeminiProvider();
            r.Initialize(GEMINIKEY);
            GUI.WriteGeneralLine("Using provided Gemini key");
            return r;
        }

        public static IButlerLLMProvider CreateOpenAIProvider(SecureString OpenAIKEY)
        {
            GUI.WriteGeneralLine("Creating OPENAI Provider");
            GUI.WriteGeneralLine("Using provided OPENAI key");
            var r = new ButlerOpenAiProvider();
            r.Initialize(OpenAIKEY);
            return r;
        }

        public static IButlerLLMProvider CreateOllamaProvider()
        {
            GUI.WriteGeneralLine("Creating Ollama Provider");
            
            var r = new OllamaOpenAiProvider(null);
            r.Initialize();
            GUI.WriteGeneralLine("No init key needed for it");
            return r;
        }

        static void GladodMode_ModelEnumeration(IButlerLLMProvider Provider )
        {
            var chat = Provider.ChatCreationProvider;
            {
                var model_enum = chat.SupportedModels;
                
                if (model_enum is null)
                {
                    GUI.WriteErrorMessage("Notice: The provider in this test section does *not* support model enumeration");
                }
                else
                {
                    GUI.WriteGeneralLine("Notice: The provider in this test section supports model listing, procueeding with it");
                    foreach (string model in  model_enum.GetEnumerator())
                    {
                        GUI.WriteGeneralLine($"MODEL:: \"{model}\"");
                    }
                }
                GUI.WriteGeneralLine("Note: This model enum test assumes Initilaize was called()");
            }
        }

        
        static Butler? Glados_MakeButlerForProvider(IButlerVaultKeyCollection vault, IButlerLLMProvider prov, string model, string KEYLOAD, IButlerChatCollection ChatHandler)
        {
            var chat = prov.ChatCreationProvider;
            if (chat  is null)
            {
                GUI.WriteErrorMessage("CRIT ERROR: This provider does not have a way to make chat factories! Unable to test!");
                return null;
            }
            else
            {
                return new Butler(vault, prov, chat.DefaultOptions, model, KEYLOAD, ChatHandler, null,null,null);
            }
        }
        /// <summary>
        /// Run the test here
        /// </summary>
        public static void GladosMode(IButlerLLMProvider Provider, string ProviderFiendly, string LLMKEYINDEX, string target_model, InMemoryApiKey? Handler)
        {
            GUI.WriteGeneralLine($"\r\n\r\n$Starting {ProviderFiendly} Test Mode using Provider {Provider.GetType().FullName}");

            InMemoryApiKey keys;
            if (Handler is null)
            {
                keys = new InMemoryApiKey();
            }
            else
            {
                GUI.WriteGeneralLine("Reusing existing API key handler");
                keys = Handler;
            }


            var Prov = Provider; // use defaultl local hard coded path

            using (SecureString key = Database.Lookup(LLMKEYINDEX))
            {

                GladodMode_ModelEnumeration(Prov);
                
                {
                    Butler? Jarvis = Glados_MakeButlerForProvider(keys, Provider, target_model, LLMKEYINDEX, new TrenchCoatChatCollection());

                    if (Jarvis is not null)
                    {
                        Jarvis.AddSystemMessage("You are a poet.");
                        Jarvis.AddSystemMessage("The current word 'pudding' is banned. Don't write poems with pudding in them");
                        Jarvis.AddSystemMessage("If you can't write a poem - reply with a haiku of why");
                        Jarvis.AddUserMessage("Please make a poem on chocolate pudding");

                        var endresult = Jarvis.StreamResponse(null, false);

                        foreach (ButlerChatMessage i in Jarvis.ChatCollection)
                        {
                            switch (i.Role)
                            {
                                case ButlerChatMessageRole.User:
                                    {
                                        GUI.WriteUserMessage($"USER:: {i.GetCombinedText()}"); break;
                                    }
                                case ButlerChatMessageRole.Assistant:
                                    {
                                        GUI.WriteGeminiLine($"AI::  {i.GetCombinedText()}"); break;
                                    }
                                case ButlerChatMessageRole.System:
                                    {
                                        GUI.WriteSystemPrompt($"SYSPROMPT:: {i.GetCombinedText()}"); break;
                                    }

                            }
                        }
                    }
                    else
                    {
                        GUI.WriteErrorMessage("ERROR unable to make instance of chat session object butler!");
                    }
                }
            }
        }
    }
}
