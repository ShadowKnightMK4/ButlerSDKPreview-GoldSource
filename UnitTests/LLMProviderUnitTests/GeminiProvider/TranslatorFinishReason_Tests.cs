using ButlerSDK.Providers.Gemini;
using ButlerToolContract.DataTypes;
using GenerativeAI.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiProvider
{
    [TestClass]
    public sealed class TranslatorFinishReason_Tests
    {
        [DataRow((FinishReason)int.MaxValue, (ButlerChatFinishReason)123412)]
        [TestMethod]
        public void TestThis_UnknownValue_ShouldException(FinishReason? Provider, ButlerChatFinishReason? General)
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorFinishReason.TranslateFromProvider(Provider);
            });
        }

        [DataRow(FinishReason.BLOCKLIST, ButlerChatFinishReason.ContentFilter)]
        [DataRow(FinishReason.RECITATION, ButlerChatFinishReason.ContentFilter)]
        [DataRow(FinishReason.SAFETY, ButlerChatFinishReason.ContentFilter)]
        [DataRow(FinishReason.IMAGE_SAFETY, ButlerChatFinishReason.ContentFilter)]
        [DataRow(FinishReason.SPII, ButlerChatFinishReason.ContentFilter)]
        [DataRow(FinishReason.STOP, ButlerChatFinishReason.Stop)]
        [DataRow(FinishReason.MAX_TOKENS, ButlerChatFinishReason.Length)]
        [DataRow(null, null)]
        [TestMethod]
        public void TestThis_ShouldMapOk(FinishReason? Provider, ButlerChatFinishReason? General)
        {
            var testthis = TranslatorFinishReason.TranslateFromProvider(Provider);
            if (testthis == null)
            {
                if (Provider == null)
                {
                    if (General == null)
                    {
                        return;
                    }
                }
            }
            if (General is null)
            {
                if (testthis is not null)
                {
                    Assert.Fail("Did not convert Transaltor to expected output");
                }
            }
            else
            {
                Assert.AreEqual(testthis, General);

            }
        }
    }
}
