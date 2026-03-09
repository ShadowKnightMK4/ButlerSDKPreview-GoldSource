using ButlerSDK.Providers.Gemini;
using ButlerSDK.Testing.Madlad;
using ButlerSDK.Tools;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;



namespace ButlerSDK.Testing.Madlad
{
    public static class ChatLogFactory
    {
        public const int SystemMessageOne = 0;
        public const int SystemMessageTwo = 1;

        /// <summary>
        /// If you add any more sys messages, set this to total for offset, Gemini Provide effectively eats them from the chat log and dumps into system instruction the -1 is to offer back into zero index array
        /// </summary>
        public const int TotalSystemMessageCount = 2 ;
        public const int UserMessageCheckIfNetworkOnline = 3;
        public const int AssistentMessageLetMeCheck = 4;
        public const int ToolCall1 = 5;
        public const int ToolReply1 = 6;
        public const int ToolCall2 = 7;
        public const int ToolReply2 = 8;
        public const int FinalAssistMessage = 9;
        public const int RefusalMessage = 10;
        public static List<ButlerChatMessage> GetComprehensiveTestLog()
        {
            return new List<ButlerChatMessage>
            {
                // 1. SYSTEM PROMPT (ButlerSystemChatMessage)    aka SystemMessageOne
                new ButlerSystemChatMessage("You are an elite AI assistant equipped with network diagnostic tools.")
                {
                    IsTemporary = false
                },

                //  2.  SYSTEM PROMPT (AKA SystemMessageTwo)
                new ButlerSystemChatMessage("You also are NOT CUSTOMER SERVICE AND ARE ROUGH AROUDN THE EDGES BUT SKILLED. - Like Dr House."),

                // 3. USER MESSAGE (ButlerUserChatMessage)
                new ButlerUserChatMessage("Hey Butler! Can you check if the network is up and get my IP?"),

                // 3. ASSISTANT MESSAGE (ButlerAssistantChatMessage - intermediate acknowledgment)
                new ButlerAssistantChatMessage("Certainly! I will run the network diagnostics and fetch your IP address right now."),

                // 4. TOOL CALL 1 (ButlerChatToolCallMessage)
                new ButlerChatToolCallMessage(
                    CallID: "call_net123",
                    ToolName: "IsInternetConnectionAvailable",
                    Args: "{}"
                )
                {
                    // Throwing in a Gemini Thought Trace just to prove the ProviderSpecific dictionary works!
                    ProviderSpecific = { { "GeminiModelThinking", "I need to ping the network first before checking the IP." } }
                },

                // 5. TOOL RESULT 1 (ButlerChatToolResultMessage)
                new ButlerChatToolResultMessage(
                    callID: "call_net123",
                    result: "{\"HasNetworkAdapter\": \"true\", \"IPv4_Ping_Success\": \"true\"}"
                )
                {
                    ToolName = "IsInternetConnectionAvailable"
                },

                // 6. TOOL CALL 2 (Testing parallel/sequential tooling)
                new ButlerChatToolCallMessage(
                    CallID: "call_ip456",
                    ToolName: "GetUserDevicePublicIP",
                    Args: "{}"
                ),

                // 7. TOOL RESULT 2
                new ButlerChatToolResultMessage(
                    callID: "call_ip456",
                    result: "203.0.113.42"
                )
                {
                    ToolName = "GetUserDevicePublicIP"
                },

                // 8. FINAL ASSISTANT MESSAGE (Standard Text)
                new ButlerAssistantChatMessage("Good news! The network is fully operational and your public IP address is 203.0.113.42."),

                // 9. THE EDGE CASE: A raw ButlerChatMessage with a Refusal Content Part
                new ButlerChatMessage()
                {
                    Role = ButlerChatMessageRole.Assistant,
                    Participant = "SafetySystem",
                    Content = new List<ButlerChatMessageContentPart>
                    {
                        new ButlerChatMessageContentPart
                        {
                            MessageType = ButlerChatMessageType.Refusal,
                            Refusal = "User requested a secondary action that violates the configured security perimeter.",
                            Text = "User requested a secondary action that violates the configured security perimeter."
                        }
                    }
                }
            };
        }
    }
}

namespace UnitTests.Provider.Gemini
{
    /// <summary>
    /// strictly speaking these tests are linked to <see cref="TranslatorChatMessage_Tests"/>. Ie if the tranlsator is broken there, these will break too
    /// </summary>
    [TestClass]
    public class TranslatorChatLog_Tests
    {
        List<ButlerChatMessage> Msgs = new();
        GenerateContentRequest? Provider;

        [TestMethod]
        public void ValidateRefusalMessage()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.RefusalMessage - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.Assistant), EvalMe.Role);

        }

        [TestMethod]
        public void ValidateFinalAssistMessage()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.FinalAssistMessage - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.Assistant), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.IsNull(EvalMe.Parts[0].FunctionCall);
            Assert.IsNull(EvalMe.Parts[0].FunctionResponse);

            Assert.IsNotEmpty(EvalMe.Parts[0].Text);
            Assert.AreEqual("Good news! The network is fully operational and your public IP address is 203.0.113.42.", EvalMe.Parts[0].Text);
        }
        [TestMethod]
        public void ValidateToolReply2_GetPubIp_Mesage()
        {            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.ToolReply2 - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolResult), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.IsNull(EvalMe.Parts[0].Text);
            Assert.IsNull(EvalMe.Parts[0].FunctionCall);
            Assert.IsNotNull(EvalMe.Parts[0].FunctionResponse);

            if (Msgs[ChatLogFactory.ToolReply2 - 1] is ButlerChatToolResultMessage ReplyMe)
            {
                Assert.AreEqual(ReplyMe.ToolName, EvalMe.Parts[0].FunctionResponse.Name);
                Assert.AreEqual(ReplyMe.Id, EvalMe.Parts[0].FunctionResponse.Id);

            }
            else
            {
                Assert.Fail("Invalidate data on the demo messages. Expected a tool reply to be a butlerchatoolreply data type.");
            }
        }

        [TestMethod]
        public void ValidateToolCall2_GetPubIp_Mesage()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.ToolCall2 - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolCall), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.IsNotNull(EvalMe.Parts[0].FunctionCall);
            Assert.IsNull(EvalMe.Parts[0].FunctionResponse);
            Assert.IsNull(EvalMe.Parts[0].Text);

            if (Msgs[ChatLogFactory.ToolCall2 - 1] is ButlerChatToolCallMessage CallMe)
            {
                Assert.AreEqual(CallMe.ToolName, EvalMe.Parts[0].FunctionCall.Name);
                Assert.AreEqual(CallMe.Id, EvalMe.Parts[0].FunctionCall.Id);

                if (CallMe.ProviderSpecific.ContainsKey(GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey))
                {
                    Assert.AreEqual(EvalMe.Parts[0].ThoughtSignature, CallMe.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey]);
                }
                else
                {
                    Console.WriteLine("WARNING: No Thought Signature for a function call. Gemini Typically don't play that. The provider did actually accept it though.");
                }



            }
            else
            {
                Assert.Fail("Invalidate data on the demo messages. Expected a tool call to be a butlerchattoolcallmessage data type.");
            }


        }
        [TestMethod]
        public void ValidateToolReply_IsInternetUP_Message()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.ToolReply1 - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolResult), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.IsNull(EvalMe.Parts[0].Text);
            Assert.IsNull(EvalMe.Parts[0].FunctionCall);
            Assert.IsNotNull(EvalMe.Parts[0].FunctionResponse);

            if (Msgs[ChatLogFactory.ToolReply1 - 1] is ButlerChatToolResultMessage ReplyMe)
            {
                Assert.AreEqual(ReplyMe.ToolName, EvalMe.Parts[0].FunctionResponse.Name);
                Assert.AreEqual(ReplyMe.Id, EvalMe.Parts[0].FunctionResponse.Id);
                
            }
            else
            {
               Assert.Fail("Invalidate data on the demo messages. Expected a tool reply to be a butlerchatoolreply data type.");
            }
        }
        [TestMethod]
        public void ValidateToolCall_IsInternetUp_Message()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.ToolCall1 - ChatLogFactory.TotalSystemMessageCount - 1];
            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolCall), EvalMe.Role);

            Assert.HasCount(1,EvalMe.Parts);
            Assert.IsNotNull(EvalMe.Parts[0].FunctionCall);
            Assert.IsNull(EvalMe.Parts[0].FunctionResponse);
            Assert.IsNull(EvalMe.Parts[0].Text);

            if (Msgs[ChatLogFactory.ToolCall1-1] is ButlerChatToolCallMessage CallMe)
            {
                Assert.AreEqual(CallMe.ToolName, EvalMe.Parts[0].FunctionCall.Name);
                Assert.AreEqual(CallMe.Id, EvalMe.Parts[0].FunctionCall.Id);

                if (CallMe.ProviderSpecific.ContainsKey(GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey) )
                {
                    Assert.AreEqual(EvalMe.Parts[0].ThoughtSignature, CallMe.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey]);
                }
                else
                {
                    Console.WriteLine("WARNING: No Thought Signature for a function call. Gemini Typically don't play that. The provider did actually accept it though.");
                }
                
                

            }
            else
            {
                Assert.Fail("Invalidate data on the demo messages. Expected a tool call to be a butlerchattoolcallmessage data type.");
            }

            
            
        }
        [TestMethod]
        public void ValidateAssistMessage_LetMeCheck()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.AssistentMessageLetMeCheck - ChatLogFactory.TotalSystemMessageCount - 1];

            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.Assistant), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.AreEqual("Certainly! I will run the network diagnostics and fetch your IP address right now.", EvalMe.Parts[0].Text);
        }
        [TestMethod]
        public void ValidateUserMessage_NetworkCheck()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Msgs);
            Content EvalMe = Provider.Contents[ChatLogFactory.UserMessageCheckIfNetworkOnline- ChatLogFactory.TotalSystemMessageCount -1];

            Assert.IsNotNull(EvalMe);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User), EvalMe.Role);

            Assert.HasCount(1, EvalMe.Parts);
            Assert.AreEqual("Hey Butler! Can you check if the network is up and get my IP?", EvalMe.Parts[0].Text);



        }
        [TestMethod]
        public void SystemPrompt_Check()
        {
            Assert.IsNotNull(Provider);
            Assert.IsNotNull(Provider.SystemInstruction);
            Assert.HasCount(1, Provider.SystemInstruction.Parts);
            Assert.Contains(Msgs[ChatLogFactory.SystemMessageOne].GetCombinedText(), Provider.SystemInstruction.Parts[0].Text);
            Assert.Contains(Msgs[ChatLogFactory.SystemMessageTwo].GetCombinedText(), Provider.SystemInstruction.Parts[0].Text);
        }

        [TestMethod]
        public void CheckIfHello_GuardAdded_StartsAsAssitentMessage()
        {
            Console.WriteLine("Gemini provider adds filler message if first message is NOT user prompt.");
            Assert.IsNotNull(Provider);
            
            GenerateContentRequest Guard = TranslatorChatLog.TranslateToProvider(new List<ButlerChatMessage>
            {
                new ButlerSystemChatMessage("You are a helpful assistant."),
                new ButlerAssistantChatMessage("Hello! How can I assist you today?")
            });

            Assert.HasCount(2, Guard.Contents);

            Assert.HasCount(1, Guard.Contents[0].Parts);
            Assert.HasCount(1, Guard.Contents[1].Parts);
            Assert.IsNotNull(Guard.Contents[0].Parts[0].Text);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User), Guard.Contents[0].Role);
            Assert.IsNotNull(Guard.Contents[1].Parts[0].Text);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.Assistant), Guard.Contents[1].Role);

            Assert.Contains("Hello", Guard.Contents[0].Parts[0].Text);
            Assert.HasCount(1, Guard.Contents[1].Parts);
            Assert.Contains("Hello! How can I assist you today?", Guard.Contents[1].Parts[0].Text);


            Assert.IsNotNull(Guard.SystemInstruction);
            Assert.HasCount(1, Guard.SystemInstruction.Parts);
            Assert.Contains("You are a helpful assistant.", Guard.SystemInstruction.Parts[0].Text);
            Assert.IsNull(Guard.SystemInstruction.Role); // seeming does better if null;
        }


        [TestMethod]
        public void CheckIfHello_GuardAdded_EmptyMessage()
        {
            Console.WriteLine("Gemini provider adds filler message if first message is NOT user prompt.");
            Assert.IsNotNull(Provider);

            GenerateContentRequest Guard = TranslatorChatLog.TranslateToProvider(new List<ButlerChatMessage>
            {
                new ButlerSystemChatMessage("You are a helpful assistant."),
            });

            Assert.HasCount(1,Guard.Contents);

            Assert.HasCount(1, Guard.Contents[0].Parts);
            Assert.IsNotNull(Guard.Contents[0].Parts[0].Text);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User), Guard.Contents[0].Role);
      

            Assert.Contains("Hello", Guard.Contents[0].Parts[0].Text);


            Assert.IsNotNull(Guard.SystemInstruction);
            Assert.HasCount(1, Guard.SystemInstruction.Parts);
            Assert.Contains("You are a helpful assistant.", Guard.SystemInstruction.Parts[0].Text);
            Assert.IsNull(Guard.SystemInstruction.Role); // seeming does better if null;
        }

        [TestInitialize]
        public void ConfigureCannedMessages()
        {
            Msgs.AddRange(ChatLogFactory.GetComprehensiveTestLog());

            Provider = TranslatorChatLog.TranslateToProvider(Msgs);

        }


    }
}
