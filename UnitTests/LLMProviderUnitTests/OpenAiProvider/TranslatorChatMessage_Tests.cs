using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Provider.OpenAI
{
    [TestClass]
    public class TranslatorChatMessage_Tests
    {

        [TestMethod]
        public void TranslateFromProvider_NullMessage_ShouldThrow()
        {
            ChatMessage? NullSector = null;
            Assert.Throws<ArgumentNullException>(() => { 
                var _ = ButlerSDK.Providers.OpenAI.TranslatorChatMessage.TranslateFromProvider(NullSector!); // supposed to be null and api reject sit
            });
        }

        [TestMethod]

        public void TranslateFromProvider_SimpleText()
        {
            string text = "Hello World!";
            ChatMessage chatMessage = ChatMessage.CreateUserMessage(text);
            Assert.IsNotNull(chatMessage);
            ButlerChatMessage butler = TranslatorChatMessage.TranslateFromProvider(chatMessage);
            Assert.IsNotNull(butler);

            Assert.HasCount(1,butler.Content);
            var part = butler.Content[0];

            Assert.AreEqual(part.Text, text);
            Assert.AreEqual(ButlerChatMessageRole.User,butler.Role);
        }



        [TestMethod]
        public void TranslateFromProvider_AudioIn_ShouldThrowExceptionNotImplemneted()
        {
            byte[] discard = new byte[5000];

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ChatMessageContentPart part = ChatMessageContentPart.CreateInputAudioPart(BinaryData.FromBytes(discard), ChatInputAudioFormat.Mp3);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            ChatMessage chat = ChatMessage.CreateAssistantMessage(part);

            Assert.IsNotNull(chat);
            Assert.Throws<NotImplementedException>(
                () => {
                    var _ = TranslatorChatMessage.TranslateFromProvider(chat);
                });
        }





        [TestMethod]
        public void TranslateFromProvider_File_ShouldThrowExceptionNotImplemneted()
        {
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ChatMessageContentPart part = ChatMessageContentPart.CreateFilePart("www");
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            ChatMessage chat = ChatMessage.CreateAssistantMessage(part);

            Assert.IsNotNull(chat);
            Assert.Throws<NotImplementedException>(
                () => {
                    var _ = TranslatorChatMessage.TranslateFromProvider(chat);
                });
        }

        [DataRow("Test method")]
        [TestMethod]
        public void TranslateToProvider_ChatMessageTextBased(string message)
        {
            void butler_assert(ButlerChatMessage x)
            {
                Assert.IsNotNull(x);
                Assert.HasCount(1, x.Content);
                Assert.AreEqual(ButlerChatMessageType.Text, x.Content[0].MessageType );
                Assert.AreEqual(message, x.Content[0].Text);

            }
            void chat_assert(ChatMessage x)
            {
                Assert.IsNotNull(x);
                Assert.HasCount(1, x.Content);

                Assert.AreEqual(ChatMessageContentPartKind.Text, x.Content[0].Kind);
                Assert.AreEqual(message, x.Content[0].Text);
            }
            ChatMessage baseline = ChatMessage.CreateAssistantMessage(message);
            Assert.IsNotNull(baseline);
            Assert.HasCount(1, baseline.Content);
            Assert.AreEqual(message, baseline.Content[0].Text);
            Assert.AreEqual(ChatMessageContentPartKind.Text, baseline.Content[0].Kind);

            var attempt = TranslatorChatMessage.TranslateFromProvider(message);
            butler_assert(attempt);
            /* we essetnail setup our 'clean' chatmessage - go translatoe. then see if back is identicle*/
            

            var back = TranslatorChatMessage.TranslateToProvider(attempt);
            chat_assert(back);

        }


        [TestMethod]
        public void TranslateFromProvider_Image_ShouldThrowExceptionNotImplemneted()
        {
            byte[] discard = new byte[5000];
            ChatMessageContentPart part = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(discard), "nottype", ChatImageDetailLevel.Auto);
            ChatMessage chat = ChatMessage.CreateAssistantMessage(part);

            Assert.IsNotNull(chat);
            Assert.Throws<NotImplementedException>(
                () => {
                    var _ = TranslatorChatMessage.TranslateFromProvider(chat);
                });
        }

        [TestMethod]
        [DataRow("“Success is not final, failure is not fatal: It is the courage to continue that counts.” – Winston Churchill")]
        public void TranslateTextMessage_BetweenButlerAndProvider_Same(string message)
        {
            void chat_assert(ChatMessage x)
            {
                Assert.IsNotNull(x);
                Assert.HasCount(1,x.Content);

                Assert.AreEqual(ChatMessageContentPartKind.Text, x.Content[0].Kind);
                Assert.AreEqual(message, x.Content[0].Text);
            }
            void butler_assert(ButlerChatMessage x)
            {
                Assert.IsNotNull(x);
                Assert.HasCount(1,x.Content);
                Assert.AreEqual(ButlerChatMessageType.Text, x.Content[0].MessageType);
                Assert.AreEqual(message, x.Content[0].Text);

            }
            AssistantChatMessage p1 = ChatMessage.CreateAssistantMessage(message);
            chat_assert(p1);
            

            var ref1 = TranslatorChatMessage.TranslateFromProvider(message);
            butler_assert(ref1);


            var backagain = TranslatorChatMessage.TranslateToProvider(ref1);

            chat_assert(backagain);
        }

        [DataRow("")]
        [DataRow("qwertyuiop[zxcvbnm,")]
        [DataRow("“Success is not final, failure is not fatal: It is the courage to continue that counts.” – Winston Churchill")]
        [TestMethod]
        public void TranslateFromProvider_SystemMessageText_Message(string MessageContents)
        {
            ChatMessage empty = ChatMessage.CreateSystemMessage(MessageContents);
            Assert.IsNotNull(empty);
            var result = TranslatorChatMessage.TranslateFromProvider(empty);
            Assert.IsNotNull(result);

            Assert.HasCount(1, empty.Content);
            Assert.HasCount(1, result.Content);


            var reference_part = empty.Content[0];
            var translated = result.Content[0];

            Assert.IsNotNull(translated);
            Assert.IsNotNull(reference_part);

            Assert.AreEqual(ButlerChatMessageType.Text,translated.MessageType);
            Assert.AreEqual(translated.Text, MessageContents);
            Assert.AreEqual(reference_part.Text, MessageContents);

           
        }



        [TestMethod]
        public void TranslatorFromProvider_RefusalMessage()
        {
            
            string text = "This content was refused by some policy.";
            ChatMessageContentPart part = ChatMessageContentPart.CreateRefusalPart(text);
            Assert.IsNotNull(part);
            Assert.AreEqual(part.Refusal, text);
            ChatMessage chat = ChatMessage.CreateAssistantMessage(part);
            Assert.IsNotNull(chat);

            var Butler = TranslatorChatMessage.TranslateFromProvider(chat);
            Assert.IsNotNull(Butler);
            Assert.HasCount(1, Butler.Content);
            var bpart = Butler.Content[0];

            Assert.AreEqual( ButlerChatMessageType.Refusal, bpart.MessageType);
            
            Assert.AreEqual(text, bpart.Refusal);



        }
    }
}
