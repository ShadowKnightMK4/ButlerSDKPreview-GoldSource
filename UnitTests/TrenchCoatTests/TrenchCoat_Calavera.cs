using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrenchCoatTests.Calavera
{
    [TestClass]
    public class TrenchCoat_Calavera
    {
        [TestMethod]
        public void TrenchCoat_DoesNotLikeNegativeIndex()
        {
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.AddUserMessage("hello");
            Assert.Throws<IndexOutOfRangeException>(() => {
                var neg_inf = test[-1];
            });
        }
        [TestMethod]
        public void TrenchCoat_DoesNotLikeNegativeIndexInfinite()
        {
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.AddUserMessage("hello");
            Assert.Throws<IndexOutOfRangeException>(() => {
                var neg_inf = test[int.MinValue];
            });
        }


        [TestMethod]
        public void TrenchCoat_ThrowsIndexOutOfRangePast_ArrayEnd()
        {
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.AddUserMessage("hello");
            test.AddSystemMessage("hi");
            test.AddPromptInjectionMessage(new ButlerChatMessage("Don't reply"), new MockPromptInjection());
            Assert.AreEqual(3, test.Count);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var neg_inf = test[int.MaxValue];
            });
        }

        [TestMethod]
        public void TrenchCoat_ThrowsIndexOutOfRangePast_ArrayEnd_OffBy1()
        {
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.AddUserMessage("hello");
            test.AddSystemMessage("hi");
            test.AddPromptInjectionMessage(new ButlerChatMessage("Don't reply"), new MockPromptInjection());
            Assert.AreEqual(3, test.Count);

            Assert.Throws<IndexOutOfRangeException>(() => {
                var neg_inf = test[3];
            });
        }


        [TestMethod]
        public void TrenchCoat_DoesNotTrim_At0()
        {
            // this tests that we don't have premature trimming
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.MaxContextWindowMessages = 0;

            test.AddAssistantMessage("TestMessage");
            Assert.AreEqual(1, test.Count);
        }

        [TestMethod]
        public void TrenchCoat_DoesNotTrim_At0_NonEmptySystem()
        {
            // this tests that we don't have premature trimming
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.MaxContextWindowMessages = 0;
            test.AddSystemMessage("SYSTEM PROMPT");
            

            test.AddAssistantMessage("TestMessage");
            Assert.AreEqual(2, test.Count);
        }


        [TestMethod]
        public void TrenchCoat_DoesNotTrim_At0_NonEmptyToolPrompt()
        {
            // this tests that we don't have premature trimming
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.MaxContextWindowMessages = 0;
            test.AddPromptInjectionMessage(new ButlerChatMessage("TOOL MESSAGE"), new MockPromptInjection());
            

            test.AddAssistantMessage("TestMessage");
            Assert.AreEqual(2, test.Count);
        }


        [TestMethod]
        public void TrenchCoat_DoesNotTrim_At0_NonEmptyToolPrompt_AndNonEmptySystemPrompt()
        {
            // this tests that we don't have premature trimming
            TrenchCoatChatCollection test = new TrenchCoatChatCollection();
            test.MaxContextWindowMessages = 0;
            test.AddPromptInjectionMessage(new ButlerChatMessage("TOOL MESSAGE"), new MockPromptInjection());
            test.AddUserMessage("Hello");
            

            test.AddAssistantMessage("TestMessage");
            Assert.AreEqual(3, test.Count);
        }
    }
}
