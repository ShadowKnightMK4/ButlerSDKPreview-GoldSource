using ButlerToolContract.DataTypes;
using OpenAI.Chat;
using ButlerSDK.Providers.OpenAI;
namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public sealed class TranslatorFinishReason_Tests
    {
        [DataRow((ChatFinishReason)int.MaxValue, (ButlerChatFinishReason) 123412)]
        [TestMethod]
        public void TestThis_UnknownValue_ShouldException(ChatFinishReason? Provider, ButlerChatFinishReason? General)
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorFinishReason.TranslateFromProvider(Provider);
            });
        }
        [DataRow(ChatFinishReason.Stop,ButlerChatFinishReason.Stop)]
        [DataRow(ChatFinishReason.ToolCalls, ButlerChatFinishReason.ToolCalls)]
        [DataRow(ChatFinishReason.FunctionCall, ButlerChatFinishReason.FunctionCall)]
        [DataRow(ChatFinishReason.Length, ButlerChatFinishReason.Length)]
        [DataRow(ChatFinishReason.ContentFilter, ButlerChatFinishReason.ContentFilter)]
        [DataRow(null, null)]
        [TestMethod]
        public void TestThis_ShouldMapOk(ChatFinishReason? Provider, ButlerChatFinishReason? General)
        {
            var testthis =  TranslatorFinishReason.TranslateFromProvider(Provider);
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
