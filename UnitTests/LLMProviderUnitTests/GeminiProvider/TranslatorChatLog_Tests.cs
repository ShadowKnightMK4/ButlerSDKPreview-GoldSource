using ButlerSDK.Providers.Gemini;
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
        [TestInitialize]
        public void ConfigureCannedMessages()
        {
            Msgs.Clear();
            // message 1  (Provider [0])
            Msgs.Add(new ButlerChatMessage("You are Helpful"));
            // message 2 (Provider = sysm prompt
            Msgs.Add(new ButlerSystemChatMessage("SECRETLY YOU ENJOY Helping and you are joyouse blue Oni about it rathar than red Oni"));

            // message 3 (Providewr is message 2)
            Msgs.Add(new ButlerChatToolCallMessage("TESTCALL", "DONOTHING", ""));
            // message 4
            {
                var addme= new ButlerChatToolResultMessage("TESTCALL", "The result of nothing was NULL.");
                addme.ToolName = "DONOTHING";
                Msgs.Add(addme); ;
            }
            

            // The code doesn't really check for if the toolname is null. ABOVE.
            

            // message 5
            Msgs.Add(new ButlerChatToolCallMessage("TESTCALL2", "FetchTheWorkChicken", ""));
            // Message 6
            Msgs.Add(new ButlerChatToolResultMessage("TESTCALL2", "Chicken"));

            // Message 7
            Msgs.Add(new ButlerAssistantChatMessage("I found the word chicken and did nothing. *giggles* anything else?"));

            // Message 8

            Msgs.Add(new ButlerUserChatMessage("Nope. That's all"));


            Provider = TranslatorChatLog.TranslateToProvider(Msgs);


        }

        [TestMethod]
        public void TranslatorToProvide_GenericList_Message8_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[6];
            Assert.IsNotNull(Msg.Parts);
            Assert.HasCount(1, Msg.Parts);
            Part P = Msg.Parts[0];
            Assert.AreEqual(P.Text, Msgs[7].Content[0].Text);
            Assert.AreEqual(ButlerChatMessageRole.User, Msgs[7].Role);


            Assert.AreEqual("user", Msg.Role);
           
        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_Message7_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[5];
            Assert.IsNotNull(Msg.Parts);
            Assert.HasCount(1, Msg.Parts);
            Part P = Msg.Parts[0];

            Assert.AreEqual(P.Text, Msgs[6].Content[0].Text);

        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_Message6_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[5];
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message5_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[4];
        }



        [TestMethod]
        public void TranslatorToProvide_GenericList_Message4_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[4];
            Assert.IsNotNull(Msg.Parts);
            Assert.HasCount(1, Msg.Parts);
            var part = Msg.Parts[0];
            Assert.IsNull(part.FunctionCall);
            Assert.IsNotNull(part.FunctionResponse);

            var reply = part.FunctionResponse;

            
            
            if (Msgs[5] is ButlerChatToolResultMessage Results)
            {
                Assert.AreEqual(Results.Id, reply.Id);
                Assert.AreEqual(Results.Message, reply.Response![0]!.ToJsonString().Trim('\"'));

            }
            else
            {
                Assert.Fail("Ge3mini translator failed to convert ok. Expected toolcallresult (butler) as source");
            }
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message3_check()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[1];
            Assert.IsNotNull(Msg.Parts);
            Assert.HasCount(1, Msg.Parts);
            var part = Msg.Parts[0];

            Assert.IsNotNull(part.FunctionCall);
            var call = part.FunctionCall;


            if (Msgs[3] is ButlerChatToolCallMessage callthis)
            {
                Assert.AreEqual(Msgs[3].Id, callthis.Id);
                if (callthis.FunctionArguments is null)
                {
                    Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("{}"), call.Args));
                }
                else
                {
                    Assert.IsTrue(JsonNode.DeepEquals(callthis.FunctionArguments, call.Args));
                }
                Assert.AreEqual(call.Name, callthis.ToolName);
                

                if (part.FunctionResponse is not null)
                {
                    Assert.Fail("Gemini Translation layer placed a function and response in the same context. They need to be sequence ie content[0] = functioncall;  content[1] = functionreply and so on");
                }

            }
            else
            {
                Assert.Fail("Gemini Translator failed to convert a butler tool call correctly. Expected the ButlerToolCallChatMessage to be converted.");
            }

            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolCall), Msg.Role);
            
        }

        [TestMethod]
        public void TranslatorToProvide_GenericList_sysprompt_ok()
        {
            Assert.IsNotNull(Provider);
            Assert.HasCount(1, Provider.SystemInstruction!.Parts);
            Assert.AreEqual(Msgs[1].Content[0].Text, Provider.SystemInstruction.Parts[0].Text);


        }
        [TestMethod]
        public void TranslatorToProvide_GenericList_CountMatches()
        {
            Assert.IsNotNull(Provider);
            Assert.HasCount(7, Provider.Contents); // we got one system message. It gets removed and placed elsewhere
        }


        [TestMethod]
        public void TranslatorToProvide_GenericList_Message1_NoRoleMeansException()
        {
            Assert.IsNotNull(Provider);
            Content Msg = Provider.Contents[0];
            ;

            Assert.Throws<Exception>(() => {
                Assert.AreEqual("user", Msg.Role);
            });
            
            Assert.IsNotNull(Msg.Parts);
            Assert.HasCount(1, Msg.Parts);
            Assert.AreEqual(Msgs[0].Content[0].Text, Msg.Parts[0].Text);
        }
        [TestMethod]
        public void TranslateToProvderEmpty_AddsHelloUserMessage()
        {
            var Generic = new List<ButlerChatMessage>();
            Assert.IsNotNull(Generic);
            Assert.IsEmpty(Generic);
            var Translated = TranslatorChatLog.TranslateToProvider(Generic);
            Assert.IsNotNull(Translated);

            Assert.IsNotEmpty(Translated.Contents);

            Assert.HasCount(1, Translated.Contents);
            Assert.AreEqual(TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User), Translated.Contents[0].Role);
            Assert.AreEqual("Hello", Translated.Contents[0].Parts[0].Text);
        }

        [TestMethod]
        public void TranslatorToProvide_NullList_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => {
                var _ = TranslatorChatLog.TranslateToProvider(null!); // supposed to be null - null throws exception
            });
        }


    }
}
