using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract;
using OpenAI.Chat;
using UnitTests.UnitTestingTools;

namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class TranslatorChatOptions_Tests
    {
        [TestMethod]
        public void DefaultToolCode_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = TranslatorChatOptions.DefaultToolCode(null!); // supposed to throw exception on getting null
            });
        }

        [TestMethod]
        public void DefaultToolCall_WorksWithMockTool()
        {
            var Mock = new NoSideEffectsTool(null);
            var TestTool = TranslatorChatOptions.DefaultToolCode(Mock);

            Assert.IsNotNull(TestTool);
            
        }

        [TestMethod]
        public void SeedToolConversion_Provider_NoTools()
        {
            IList<IButlerToolBaseInterface> x = new List<IButlerToolBaseInterface>();
            IList < ChatTool> chatTools = new List<ChatTool>();

            Assert.IsNotNull(x);
            Assert.IsNotNull(chatTools);


            TranslatorChatOptions.SeedToolConversionToProvider(x, chatTools);


            Assert.HasCount(0, x);
            Assert.HasCount(0, chatTools);
        }

        [TestMethod]
        public void SeedtoolConversionProvider_SingleTool_RESETOK_ShouldOnlyHaveSingleTool()
        {
            // set the data types
            NoSideEffectsTool NewTool = new NoSideEffectsTool(null);
            IList<IButlerToolBaseInterface> x = new List<IButlerToolBaseInterface>();
            IList<ChatTool> chatTools = new List<ChatTool>();

            Assert.IsNotNull(x);
            Assert.IsNotNull(chatTools);
            // prefill with dummy names
            chatTools.Add(ChatTool.CreateFunctionTool("DUMMY1"));
            chatTools.Add(ChatTool.CreateFunctionTool("DUMMY2"));
            x.Add(NewTool);

            // sain check
            Assert.HasCount(2,chatTools);
            Assert.HasCount(1, x);

            TranslatorChatOptions.SeedToolConversionToProvider(x, chatTools, false);

            // verify the routine cleared the current toolset
            Assert.HasCount(1, chatTools);

            // and the toolname matches
            Assert.AreEqual(chatTools[0].FunctionName,NewTool.ToolName);
        }

        [TestMethod]
        public void SeedtoolConversionProvider_SingleTool_DONOTRESET_ShouldOnlyHaveTHREETool()
        {
            NoSideEffectsTool NewTool = new NoSideEffectsTool(null);
            IList<IButlerToolBaseInterface> x = new List<IButlerToolBaseInterface>();
            IList<ChatTool> chatTools = new List<ChatTool>();

            Assert.IsNotNull(x);
            Assert.IsNotNull(chatTools);
            chatTools.Add(ChatTool.CreateFunctionTool("DUMMY1"));
            chatTools.Add(ChatTool.CreateFunctionTool("DUMMY2"));
            x.Add(NewTool);

            Assert.HasCount(2, chatTools);
            Assert.HasCount(1,x);

            TranslatorChatOptions.SeedToolConversionToProvider(x, chatTools, true);

            Assert.HasCount(3,chatTools);


            bool d1, d2, ns;
            d1 = d2 = ns = false;
            foreach (var tool in chatTools)
            {
                if (tool.FunctionName == NewTool.ToolName)
                    ns = true;
                if (tool.FunctionName == "DUMMY1") 
                    d1 = true;
                if (tool.FunctionName == "DUMMY2")
                    d2 = true;
            }

            Assert.IsTrue(d1);
            Assert.IsTrue(d2);
            Assert.IsTrue(ns);
        }

        /* I GOT TO REMEMBER WHAT I WANTED TO DO HERE LATER
         1. Create a provider object
         2. Create a chat options object
         3. Convert to provider chat options
         4. Convert back from provider chat options
         5. Verify the two chat options objects match in data
         
        [TestMethod]
        public void TranslatorFromProvider()
        {
            Assert.Fail("TODO");
        }

        public void TranslatorToProvider()
        {
            Assert.Fail("TODO");
        }*/
    }
}
