using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;


namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class ReasoningEffortUnitTest
    {
        /*            if (X.ReasoningEffortLevel == ChatReasoningEffortLevel.High)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Max;
            } else if (X.ReasoningEffortLevel ==  ChatReasoningEffortLevel.Medium)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Medium;
            } else if (X.ReasoningEffortLevel == ChatReasoningEffortLevel.Low)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Low;
            } else if (X.ReasoningEffortLevel ==  ChatReasoningEffortLevel.None)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.None;
            } else if (X.ReasoningEffortLevel == null)
            {
                ret.ReasoningEffort = null;
            }
            else
            {
                throw new InvalidDataException("Unknown reasoning effort. Accepted values are {High, medium, low, none} if openai has added more, this provider for butler needs update");
            }*/

        [TestMethod]
        public void ReasoningEffort_SimpleCheck()
        {
            ButlerOpenAiProvider x = new();
            var Options = x.ChatCreationProvider.DefaultOptions;

            try
            {
                var hasField = Options.GetType().GetField("ReasoningEffort");
            }
            catch (Exception ex)
            {
                Assert.Fail("Error: The Openai provider does not the ReasoningEffort Field or something broke it. Check interface and contents.");
            }
        }

        [TestMethod]
        public void ReasoningEffort_AcceptsSigning()
        {
            ButlerOpenAiProvider x = new();
            var Options = x.ChatCreationProvider.DefaultOptions;

            var reason = Options.ReasoningEffort;

            Options.ReasoningEffort = ButlerThinkingEffortChoice.Low;

            Assert.AreEqual(ButlerThinkingEffortChoice.Low, Options.ReasoningEffort);


            Options.ReasoningEffort = ButlerThinkingEffortChoice.Max;

            Assert.AreEqual(ButlerThinkingEffortChoice.Max, Options.ReasoningEffort);

            Options.ReasoningEffort = ButlerThinkingEffortChoice.Medium;

            Assert.AreEqual(ButlerThinkingEffortChoice.Medium, Options.ReasoningEffort);

            Options.ReasoningEffort = ButlerThinkingEffortChoice.High;

            Assert.AreEqual(ButlerThinkingEffortChoice.High, Options.ReasoningEffort);

            Options.ReasoningEffort = null;
            Assert.IsNull(Options.ReasoningEffort);
        }
    }
}
