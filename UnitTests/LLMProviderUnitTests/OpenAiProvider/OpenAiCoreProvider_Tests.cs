using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.OpenAI;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using OpenAI;
using OpenAI.Chat;
using UnitTests.UnitTestingTools;
namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class OpenAiCoreProvider_Tests
    {
        [TestMethod]
        public void CanActuallyMakeInstance_IsNotNUll()
        {
            
            ButlerOpenAiProvider x = new ButlerOpenAiProvider();
            Assert.IsNotNull(x);
            Assert.IsInstanceOfType(x, typeof(IButlerLLMProvider));
        }

        static bool containstool(string name, IList<ChatTool> check)
        {
            bool found = false;
            foreach (ChatTool tool in check)
            {
                if (string.Compare(tool.FunctionName, name, true) == 0) { found = true; break; }
            }
            return found;
        }
        static void ToolConvert(IButlerChatCompletionOptions y, ChatCompletionOptions x)
        {
            if ( (y.Tools.Count == 0) && (x.Tools.Count == 0))
            {
                return; // no to compare
            }
            if ((y.Tools.Count != 0) && (x.Tools.Count == 0))
            {
                Assert.Fail("Toolcount:  ButlerChatCompletion had tools in list, openai provider did not (was it tranlsated ok");
            }

            if ((x.Tools.Count != 0) && (y.Tools.Count == 0))
            {
                Assert.Fail("Toolcount:  ButlerChatCompletion had  NO tools in list, openai provider did have  (was it tranlsated ok?)");
            }

            if (y.Tools.Count != x.Tools.Count)
            {
                Assert.Fail("Mismatch tool count. Either butler chat options or openai chat options has an excess tool or missing one. Each tool in butler chat chould pair to openai provider");
            }

            foreach (var toolbutler in y.Tools)
            {
                Assert.IsNotNull(toolbutler);
                Assert.IsTrue(containstool(toolbutler.ToolName, x.Tools));
            }
        }

        [TestMethod]
        public void ChatCreationProviderTest_ShouldNotBeNull()
        {
            ButlerOpenAiProvider x = new();
            Assert.IsNotNull(x);
            IButlerChatCreationProvider Provider = x.ChatCreationProvider;
            Assert.IsNotNull(Provider);
        }
        [TestMethod]
        public void CreateChatToolWorks_ShouldMatch()
        {
            ButlerOpenAiProvider x = new();
            Assert.IsNotNull(x);
            IButlerToolBaseInterface testtool = new NoSideEffectsTool(null);
            Assert.IsNotNull(testtool);

            ChatTool convert_tool = (ChatTool)x.CreateChatTool(testtool);

            Assert.IsNotNull(convert_tool);
            Assert.IsInstanceOfType(convert_tool, typeof(ChatTool));

            Assert.AreEqual(convert_tool.FunctionDescription, testtool.ToolDescription);
            Assert.AreEqual(convert_tool.FunctionName, testtool.ToolName);

            var datajson = convert_tool.FunctionParameters.ToString();
            Assert.IsNotNull(datajson);
            Assert.AreEqual(datajson, testtool.GetToolJsonString());
        }
        [TestMethod]
        public void TranslatorDefaultOptions_BackAndForth_ShouldMatch()
        {
            Console.WriteLine("TranslatorDefaultOptions_BackAndForth_ShouldMatch() works a little different. If it throws an exception that's a single text with no spaces- the conversion routine failed to convert *that* data type in the chat options ok");
            ButlerOpenAiProvider x = new ButlerOpenAiProvider();

            Assert.IsNotNull(x);
            var Options = x.DefaultOptions;
            Assert.IsNotNull (Options);
            ChatCompletionOptions Default = new ChatCompletionOptions();
            var VerifyConvert = TranslatorChatOptions.TranslateToProvider(Options, x);

            Assert.IsNotNull (VerifyConvert);
            
            if (Options.PresencePenalty != VerifyConvert.PresencePenalty)
            {
                Assert.Fail("PresencePenalty");
            }
            if (Options.TopP != VerifyConvert.TopP) { Assert.Fail("TopP"); }
            
            switch (Options.ToolChoice)
            {
                case ButlerToolContract.DataTypes.ButlerChatToolChoice.None:
                    {
                        Assert.AreEqual(VerifyConvert.ToolChoice, ChatToolChoice.CreateNoneChoice());
                        break;
                    }
                case ButlerToolContract.DataTypes.ButlerChatToolChoice.Auto:
                    {
                        Assert.AreEqual(VerifyConvert.ToolChoice, ChatToolChoice.CreateAutoChoice());
                        break;
                    }
                case ButlerToolContract.DataTypes.ButlerChatToolChoice.Required:
                    {
                        Assert.AreEqual(VerifyConvert.ToolChoice, ChatToolChoice.CreateRequiredChoice());
                        break;
                    }
                case null:
                    {
                        Assert.IsNull(VerifyConvert.ToolChoice);
                        break;
                    }
                default:
                    {
                        Assert.Fail("Possibly chat openai added a new tool choice. It's not currently understood by trransaltor");
                        break;
                    }
            }

            if (Options.Temperature != VerifyConvert.Temperature) { Assert.Fail("Temperature"); }

            if (Options.StopSequences.Count != 0)
            {
                if (VerifyConvert.StopSequences.Count == 0)
                {
                    Assert.Fail("StopMessage was 0 in converted message but not original");
                }
                else
                {
                    foreach (var part in Options.StopSequences)
                    {
                        if (VerifyConvert.StopSequences.Contains(part) == false)
                        {
                            Assert.Fail("Converted stop sequence (butler one) has something added original stop sequence didn't");
                        }
                    }

                    foreach (var part in VerifyConvert.StopSequences)
                    {
                        if (Options.StopSequences.Contains(part) == false)
                        {
                            Assert.Fail("Converted stop sequence (butler one) does NOT contain text the original stop sequence did");
                        }
                    }
                }
            }
            else
            {
                if (VerifyConvert.StopSequences.Count != 0)
                {
                    Assert.Fail("StopSequence is null in butler message but not chat message. Check if tranlsator mapped the stop message ok.");
                }
            }

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            Assert.AreEqual(VerifyConvert.Seed, Options.Seed);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


            Assert.AreEqual(Options.PresencePenalty, VerifyConvert.PresencePenalty);

            Assert.AreEqual(Options.MaxOutputTokenCount, VerifyConvert.MaxOutputTokenCount);
            Assert.AreEqual(Options.FrequencyPenalty, VerifyConvert.FrequencyPenalty);
            Assert.AreEqual(Options.EndUserId, VerifyConvert.EndUserId);
            Assert.AreEqual(Options.AllowParallelToolCalls, VerifyConvert.AllowParallelToolCalls);

            ToolConvert(Options, VerifyConvert);
        }

        
    }
}
