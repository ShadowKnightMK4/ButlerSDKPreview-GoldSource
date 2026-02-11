using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerToolContract;
using ButlerToolContract.DataTypes;

namespace TrenchCoatTests
{
    [TestClass]
    public class TrenchCoutUnitTests_AgingOutTests
    {
        [TestMethod]
        public void TrenchCoatChat_AcceptsAgeValue_positive()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);
            TC.MaxContextWindowMessages = 5;
            Assert.AreEqual(5, TC.MaxContextWindowMessages);
        }


        [TestMethod]
        public void TrenchCoatChat_AcceptsSpecialCase_Unlimited()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);
            TC.MaxContextWindowMessages = TrenchCoatChatCollection.UnlimitedContextWindow;
            Assert.AreEqual(TrenchCoatChatCollection.UnlimitedContextWindow, TC.MaxContextWindowMessages);
        }

        [TestMethod]
        public void TrenchCoatChat_AcceptsAgeValue_Zero()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);
            TC.MaxContextWindowMessages = 0;
            Assert.AreEqual(0, TC.MaxContextWindowMessages);
        }

        [TestMethod]
        public void TrenchCoatChat_ThrowsOnNegativeAge()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);
            TC.MaxContextWindowMessages = TrenchCoatChatCollection.UnlimitedContextWindow;
            
            Assert.Throws<InvalidOperationException>(() =>
            {
                TC.MaxContextWindowMessages = int.MinValue;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                TC.MaxContextWindowMessages = int.MinValue/2;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                TC.MaxContextWindowMessages = int.MinValue/2/2;
            });
        }

        [TestMethod]
        public void TrenchCoatChat_DoesNotTrimOldMessageWithoutAdd()
        {
            TrenchCoatChatCollection TC = new ();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);

            TC.AddUserMessage(("User Message 1"));
            TC.AddUserMessage(("User Message 2"));
            TC.AddUserMessage(("User Message 3"));
            TC.AddUserMessage(("User Message 4"));

            Assert.AreEqual(4, TC.RunningContextWindowCount);
        }

        [TestMethod]
        public void TrenchCoat_TrimsMessagesOnAdd_NoToolcallState()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);

            TC.MaxContextWindowMessages = 2;
            Assert.AreEqual(2, TC.MaxContextWindowMessages); // verify set ok

            TC.AddUserMessage(("User Message 1")); // does not trigger trim
            TC.AddUserMessage(("User Message 2")); // does not trigger trim
            TC.AddUserMessage(("User Message 3")); // triggers trim (message 1 should be dropped)

            Assert.AreEqual(2, TC.RunningContextWindowCount); // verify trim happened
            Assert.AreEqual("User Message 2", TC[0].GetCombinedText()); // verify message 1 dropped


            TC.AddUserMessage(("User Message 4"));

            Assert.AreEqual(2, TC.RunningContextWindowCount); // verify trim still happened
            Assert.AreEqual("User Message 3", TC[0].GetCombinedText()); // verify message 2 dropped


            Assert.AreEqual("User Message 4", TC[1].GetCombinedText()); // verify message 4 present

        }


     


            [TestMethod]
        public void TrenchCoat_TrimsMessagesOnAdd_HasTools_TrimsTools()
        {
            TrenchCoatChatCollection TC = new();
            Assert.AreEqual(TrenchCoatChatCollection.DefaultMaxContextWindowMessages, TC.MaxContextWindowMessages);

            TC.MaxContextWindowMessages = 3;
            Assert.AreEqual(3, TC.MaxContextWindowMessages); // verify set ok


            TC.AddUserMessage(("User Message 1")); // does not trigger trim
            TC.AddUserMessage(("User Message 2")); // does not trigger trim
            TC.AddUserMessage(("User Message 3")); // does not trigger trim

            var call1 = new ButlerChatToolCallMessage("TEST", "TestTool", "{}");
            TC.Add(call1);
            Assert.AreEqual("User Message 2", TC[0].GetCombinedText()); // verify message 1 dropped
            


            var reply1 = new ButlerChatToolResultMessage("TEST", "YES");
            TC.Add(reply1);
            Assert.AreEqual("User Message 3", TC[0].GetCombinedText()); // verify message 2 dropped
            


            TC.AddUserMessage(("User Message 4")); // triggers trim. (message 3 should be dropped)
            Assert.AreEqual("TestTool", ((ButlerChatToolCallMessage)TC[0]).ToolName); // verify message 3 dropped
            Assert.AreEqual("TEST", ((ButlerChatToolCallMessage)TC[0]).Id);

            TC.AddUserMessage(("User Message 5")); // this should trigger trim (call1 and reply1 should be dropped)
            Assert.AreEqual("User Message 4", TC[0].GetCombinedText()); // verify message 2 dropped



            TC.AddUserMessage(("User Message 6")); // / does not trigger trim (the tool calls count as 2)
            Assert.AreEqual("User Message 4", TC[0].GetCombinedText()); // verify message NO MESSAGE WAS DROPPED. Hence the duplicate assert here (as seen on line 141aka -2 lines)


            var call2 = new ButlerChatToolCallMessage("TEST2", "TestTool", "{}");
            var reply2 = new ButlerChatToolResultMessage("TEST2", "YES");
            TC.Add(call2);
            Assert.AreEqual("User Message 5", TC[0].GetCombinedText()); // verify message 4 dropped
            

            TC.Add(reply2);
            Assert.AreEqual("User Message 6", TC[0].GetCombinedText()); // verify message 5 dropped
            
            TC.AddUserMessage(("User Message 7")); // does not trigger trim
            Assert.AreEqual("TestTool", ((ButlerChatToolCallMessage)TC[0]).ToolName); // verify message 3 dropped
            Assert.AreEqual("TEST2", ((ButlerChatToolCallMessage)TC[0]).Id);
            TC.AddUserMessage(("User Message 8")); // does not trigger trim
            TC.AddUserMessage(("User Message 9")); // triggers trim (message 1 should be dropped)


            Assert.AreEqual("User Message 7", TC[0].GetCombinedText());
            Assert.AreEqual("User Message 8", TC[1].GetCombinedText());
            Assert.AreEqual("User Message 9", TC[2].GetCombinedText());


        }
    }
}
