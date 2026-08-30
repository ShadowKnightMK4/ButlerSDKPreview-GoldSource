using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ApiKeyMgr;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.Core;
using ButlerSDK.Providers.Gemini;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using SecureStringHelper;

namespace TheWorks.GladosTest
{
    internal static class GeminiTest
    {
        public static void GladDosMode(InMemoryApiKey? Handler)
        {
            var common = CommonGroundGladosTest.CreateGeminiProvider(CommonGroundGladosTest.LoadKey("GEMINI"));

            CommonGroundGladosTest.GladosMode(common, "Gemini Provider", "GEMINI", CommonGroundGladosTest.GEMINI_MODEL_TEST, Handler);
        }
        /// <summary>
        /// Run the test here
        /// </summary>
        public static void GladosMode_old(InMemoryApiKey? Handler)
        {
            GUI.WriteGeneralLine("\r\n\r\nStarting Gemini Test Mode");

            InMemoryApiKey keys;
            if (Handler is null)
            {
                keys = new InMemoryApiKey();
            }
            else
            {
                GUI.WriteGeneralLine("Reusing existing handler");
                keys = Handler;
            }


            var Prov = new ButlerGeminiProvider(); // use defaultl local hard coded path

            using (SecureString key = Database.Lookup("GEMINI"))
            {
                GUI.WriteGeneralLine("Initialiing Gemini Provider in API key mode");
                Prov.Initialize(key);

                var support = Prov.SupportedModels;

                var target_model = string.Empty;
                if (support is not null)
                {
                    GUI.WriteGeneralLine("The provider supports model enumeration. Model List is below.");
                    foreach (string model in support.GetEnumerator())
                    {
                        target_model = model;
                        GUI.WriteGeneralLine($"Model:: {model}");
                    }
                }
                else
                {
                    GUI.WriteErrorMessage("The provider does not support model enumeration");
                    GUI.WriteErrorMessage("Unable to continue with the works test");
                    return;
                }

                target_model = "models/gemini-3.7-flash";
                {
                    Butler Jarvis = new Butler(keys, Prov, Prov.DefaultOptions, target_model, "GEMINI", null, null);
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
            }
        }
    }
}
