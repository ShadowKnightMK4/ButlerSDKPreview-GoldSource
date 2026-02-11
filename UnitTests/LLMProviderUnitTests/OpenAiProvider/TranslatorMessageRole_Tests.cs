using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class TranslatorMessageRole_Tests
    {



        [DataRow(ChatMessageRole.Function, ButlerChatMessageRole.ToolCall)]
        [TestMethod]
        
        public void FromProvider_FunctionNotMapped_ShouldThrowNotImplement(ChatMessageRole? Provider, ButlerChatMessageRole gen)
        {


            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorRole.TranslateFromProvider(Provider);
            });
           
        }

        [DataRow((ChatMessageRole)int.MaxValue, null)]
        [TestMethod]
        public void FromProvider_UnknownValu_ShouldThrowNotImplement(ChatMessageRole? Provider, ButlerChatMessageRole? gen)
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                var testthis = TranslatorRole.TranslateFromProvider(Provider);
            });
        }

        [DataRow(ChatMessageRole.System, ButlerChatMessageRole.System)]
        [DataRow(ChatMessageRole.User, ButlerChatMessageRole.User)]
        [DataRow(ChatMessageRole.Assistant, ButlerChatMessageRole.Assistant)]
        [DataRow(ChatMessageRole.Tool, ButlerChatMessageRole.ToolCall)]
        [DataRow(null, null)]
        [TestMethod]
        public void FromPRovider_ShouldMapOk(ChatMessageRole? Provider, ButlerChatMessageRole? gen)
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
