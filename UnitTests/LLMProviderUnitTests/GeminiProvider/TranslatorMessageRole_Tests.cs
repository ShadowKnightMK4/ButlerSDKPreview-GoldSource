using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ButlerSDK.Providers.Gemini;
namespace GeminiProvider
{
    [TestClass]
    public class TranslatorMessageRole_Tests
    {



        [DataRow("system", ButlerChatMessageRole.ToolCall)]
        [TestMethod]

        public void FromProvider_SystemHasNoDirectTranslation_ShouldThrowNotImplement(string? Provider, ButlerChatMessageRole gen)
        {


            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorRole.TranslateFromProvider(Provider);
            });

        }

        [DataRow((string?)"qwertyhujkisdaik", null)]
        [TestMethod]
        public void FromProvider_UnknownValu_ShouldThrowNotImplement(string? Provider, ButlerChatMessageRole? gen)
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorRole.TranslateFromProvider(Provider);
            });
        }

        [DataRow("user", ButlerChatMessageRole.User)]
        [DataRow("model", ButlerChatMessageRole.Assistant)]
        //[DataRow("function", ButlerChatMessageRole.ToolCall)] 
        [DataRow("function", ButlerChatMessageRole.ToolResult)]
        [DataRow(null, null)]
        [TestMethod]
        public void FromPRovider_ShouldMapOk(string? Provider, ButlerChatMessageRole? gen)
        {


            var testthis = TranslatorRole.TranslateFromProvider(Provider);


            if (gen is null)
            {
                if (Provider is null)
                {
                    return;
                }
            }
            Assert.AreEqual(testthis, gen);
        }
    }
}
