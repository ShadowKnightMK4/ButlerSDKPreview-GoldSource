using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
/*
 * Contains the usual LLMProvider, ect.... that all providers have
 */ 
namespace ButlerSDK.Provider.OpenAI
{

    public class ButlerOpenAiChatClient : IButlerChatClient
    {
        ChatClient MyClient;
        IButlerLLMProvider ProviderSource;
        IButlerChatPreprocessor? PPR = null;
        internal ButlerOpenAiChatClient(OpenAIClient x, string Model, IButlerLLMProvider Source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Model);
            ArgumentNullException.ThrowIfNull(x);
            MyClient = x.GetChatClient(Model);
            this.ProviderSource = Source;
            this.PPR = DefaultOpenAiProvider.Instance;
        }

        internal ButlerOpenAiChatClient(ChatClient myClient, IButlerLLMProvider Source, IButlerChatPreprocessor? PPR)
        {

            ArgumentNullException.ThrowIfNull(Source);
            ArgumentNullException.ThrowIfNull(myClient);
            MyClient = myClient;
            this.ProviderSource = Source;
            this.PPR = PPR;
        }


        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            IList<ButlerChatMessage> PPRMSG;
            if (PPR is not null)
            {
                PPRMSG = PPR.PreprocessMessages(msg);
            }
            else
            {
                PPRMSG = msg;
            }
            var tmplog = TranslatorChatLog.TranslateToProvider(PPRMSG);
            if (tmplog is null)
            {
                throw new ArgumentException("Translation layer between OpenAI provider and butler failed");
            }
            var result = MyClient.CompleteChat(tmplog);

            return new ButlerOpenAiClientResult(result);
        }

        public async IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, [EnumeratorCancellation] CancellationToken cancelMe = default)
        {
            IList<ButlerChatMessage> PPRMSG;
            if (PPR is not null)
            {
                PPRMSG = PPR.PreprocessMessages(msg);
            }
            else
            {
                PPRMSG = msg;
            }

            List<ChatMessage> ProviderFormat = (List<ChatMessage>)TranslatorChatLog.TranslateToProvider(PPRMSG);
            ChatCompletionOptions ProviderOptions = TranslatorChatOptions.TranslateToProvider(options, ProviderSource);
            AsyncCollectionResult<StreamingChatCompletionUpdate> Result = MyClient.CompleteChatStreamingAsync(ProviderFormat, ProviderOptions, cancelMe);
 
                await foreach (var part in Result.WithCancellation(cancelMe))
                {
                    if (part is not null)
                    {
                        var butlerpart = TranslatorStreamingChatUpdate.TranslateFromProvider(part);
                        yield return butlerpart;
                    }
                    continue;
                }

        }
        public IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            return CompleteChatStreamingAsync(msg, options, default);
        }

        IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> IButlerChatClient.CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            IList<ButlerChatMessage> PPRMSG;
            if (PPR is not null)
            {
                PPRMSG = PPR.PreprocessMessages(msg);
            }
            else
            {
                PPRMSG = msg;
            }

            List<ChatMessage> ProviderFormat = (List<ChatMessage>)TranslatorChatLog.TranslateToProvider(PPRMSG);
            ChatCompletionOptions ProviderOptions = TranslatorChatOptions.TranslateToProvider(options, ProviderSource);
            var Result = MyClient.CompleteChatStreaming(ProviderFormat, ProviderOptions);
            return new ButlerOpenAiCollectionResult<ButlerStreamingChatCompletionUpdate>(Result);
        }
    }
    ///// <summary>
    ///// The provider you pass to Butler5 to use an OpenAI API with there servers
    ///// </summary>
    //public class ButlerOpenAIProvider : IButlerChatCreationProvider
    //{
    //    OpenAIClient _client = null;
    //    ChatCompletionOptions MainOptions;

    //    public IButlerChatCompletionOptions DefaultOptions
    //    {
    //        get
    //        {
    //            if (MainOptions is null)
    //            {
    //                MainOptions = new ChatCompletionOptions();
    //                return MainOptions as IButlerChatCompletionOptions;
    //            }
    //            return MainOptions as IButlerChatCompletionOptions;
    //        }
    //    }

    //    public IButlerChatClient GetChatClient(string mode0l, object? Options)
    //    {
    //        ChatClient Chat;

    //        Chat = _client.GetChatClient(model);
    //        // make the common chat end point for personality and general prompts
    //        MainOptions = new();
    //        return new ButlerOpenAiChatClient(Chat);
    //    }
    //    public void Initialize(SecureString x)
    //    {
    //        _client = new OpenAIClient(x.DecryptString());
    //        if (_client is null)
    //        {
    //            throw new ArgumentNullException(nameof(_client), "OpenAI Client could not be initialized. Please check your API key.");
    //        }

    //    }
    //}


    public class ButlerOpenAiCollectionResult<T> : IButlerCollectionResult<ButlerStreamingChatCompletionUpdate>
    {
        CollectionResult<StreamingChatCompletionUpdate> Update;
#if DEBUG
        bool resetmode = false;
        List<StreamingChatCompletionUpdate> Parts = new();
#endif
        public ButlerOpenAiCollectionResult(CollectionResult<StreamingChatCompletionUpdate> ProviderUpdate)
        {
            Update = ProviderUpdate;
        }
        public IEnumerator<ButlerStreamingChatCompletionUpdate> GetEnumerator()
        {

            var ProviderUpdateEnum = Update.GetEnumerator();

            while (ProviderUpdateEnum.MoveNext())
            {
                StreamingChatCompletionUpdate ThisOne = ProviderUpdateEnum.Current;
#if DEBUG
                resetmode = false;
                if (Parts.Count != 0)
                {
                    if (resetmode)
                    {
                        Parts.Clear();
                        Parts.Add(ThisOne);
                    }
                }
                else
                {
                    Parts.Add(ThisOne);
                }
#else
               // Parts.Add(ThisOne);
#endif
                var ButlerVarient = TranslatorStreamingChatUpdate.TranslateFromProvider(ThisOne);
                yield return ButlerVarient;
            }
            //   Parts.Clear();
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public class ButlerOpenAiAsyncCollectionResult : IButlerAsynchCollectionResult<ButlerStreamingChatCompletionUpdate>
    {
        AsyncCollectionResult<StreamingChatCompletionUpdate> Update;
        public ButlerOpenAiAsyncCollectionResult(AsyncCollectionResult<StreamingChatCompletionUpdate> ProviderUpdate)
        {
            Update = ProviderUpdate;
        }
        public async IAsyncEnumerator<ButlerStreamingChatCompletionUpdate> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var part in Update.WithCancellation(cancellationToken))
            {
                var butlerpart = TranslatorStreamingChatUpdate.TranslateFromProvider(part);
                yield return butlerpart;
            }
        }


    }

#pragma warning disable OPENAI001

    /// <summary>
    /// Implements <see cref="IButlerClientResult"/> without directly exposing <see cref="ClientResult"/> to butler;
    /// </summary>
    public class ButlerOpenAiClientResult : IButlerClientResult
    {
        ResponseResult? Response;
        ClientResult<ChatCompletion>? ClientResult;
        public ButlerOpenAiClientResult(ClientResult<ChatCompletion> ClientResult)
        {
            ArgumentNullException.ThrowIfNull(nameof(ClientResult));
            this.ClientResult = ClientResult;
        }
        public ButlerOpenAiClientResult(ResponseResult ResponseResult)
        {
            ArgumentNullException.ThrowIfNull(nameof(ResponseResult));
        }

        // <summary>
        /// Gets the contents of the client result as an array of bytes
        /// </summary>
        /// <returns></returns>
        public byte[]? GetBytes()
        {
            if (Response is not null)
            {
                throw new NotImplementedException();
            }
            return ClientResult.GetRawResponse().Content.ToArray();
        }

        /// <summary>
        /// Gets the client result as a string
        /// </summary>
        /// <returns></returns>
        public string? GetResult()
        {
            if (ClientResult is not null)
                return ClientResult.ToString();
            else
                if (Response is not null)
                {
                    return Response.GetOutputText();
                }
                else
                {
                    throw new InvalidOperationException("Both Client and repsonse are null. This is not normal");
                }
        }

        public ButlerClientResultType GetResultType()
        {
            return ButlerClientResultType.String;
        }
    }
#pragma warning restore OPENAI001



    /// <summary>
    /// Convert a list of <see cref="ButlerChatMessage"/> to <see cref="ChatMessage"/> and back
    /// </summary>
    public static class TranslatorChatLog
    {


        public static IList<ChatMessage> TranslateToProvider(IList<ButlerChatMessage> ChatLog)
        {
            ArgumentNullException.ThrowIfNull(ChatLog, nameof(ChatLog));
            List<ChatMessage> ret = new List<ChatMessage>();
            for (int i = 0; i < ChatLog.Count; i++)
            {
                var message = TranslatorChatMessage.TranslateToProvider(ChatLog[i]);
                ret.Add(message);
            }
            return ret;
        }

        public static IList<ButlerChatMessage> TranslateFromProvider(IList<ChatMessage> ChatLog)
        {
            List<ButlerChatMessage> ret = new();
            foreach (ChatMessage x in ChatLog)
            {

#if DEBUG
                ButlerChatMessage y = TranslatorChatMessage.TranslateFromProvider(x);
                if (y is ButlerChatToolCallMessage)
                {
                    ;
                }
                ret.Add(y);
#else
            ret.Add(TranslatorChatMessage.TranslateFromProvider(x));
#endif
            }
            return ret;
        }

    }



    public static class TranslatorStreamingChatUpdate
    {
        public static ButlerChatStreamingPart TranslatorFromProvider(ChatMessageContentPart part)
        {
            var ret = new ButlerChatStreamingPart();
            ret.Text = part.Text;

            switch (part.Kind)
            {
                case ChatMessageContentPartKind.Text: ret.Kind = ButlerChatMessagePartKind.Text; break;
                case ChatMessageContentPartKind.Refusal: ret.Kind = ButlerChatMessagePartKind.Refusal; break;
                case ChatMessageContentPartKind.Image: ret.Kind = ButlerChatMessagePartKind.Image; break;
                default: Debugger.Break(); break;

            }

            /*
             * DEAR FUTURE SELF. expand butler's chat stream part to support this part.
             * currently butler does text only.
            part.FileBytes;
            part.FileBytesMediaType;
            part.FileId;
            part.Filename;
            part.ImageBytes;
            part.ImageBytesMediaType;
            part.ImageDetailLevel;
            part.InputAudioBytes;
            part.InputAudioFormat;
            part.Kind;
            part.Refusal;
            part.Text;
            */
            return ret;
        }




        public static ButlerStreamingChatCompletionUpdate TranslateFromProvider(StreamingChatCompletionUpdate Part, bool DiscardNulLContentParts = true)
        {
            ButlerStreamingChatCompletionUpdate ret = new();
            //ret.FunctionArgumentsUpdate = Part.FunctionCallUpdate;
            //ret.ContentUpdate;


            ret.CompletionId = Part.CompletionId;
            //Part.ContentTokenLogProbabilities;
            foreach (ChatMessageContentPart P in Part.ContentUpdate)
            {
                if (P.Kind == ChatMessageContentPartKind.Text)
                {
                    if (!string.IsNullOrEmpty(P.Text))
                    {
                        ret.EditorableContentUpdate.Add(TranslatorFromProvider(P));
                    }
                }
                else
                {
                    ret.EditorableContentUpdate.Add(TranslatorFromProvider(P));
                }

            }

            foreach (StreamingChatToolCallUpdate P in Part.ToolCallUpdates)
            {
                ret.EditableToolCallUpdates.Add(TranslatorStreamingChatToolCalls.TranslateFromProvider(P));
            }
            ret.CreatedAt = Part.CreatedAt;
            ret.FinishReason = TranslatorFinishReason.TranslateFromProvider(Part.FinishReason);
            ret.Model = Part.Model;
            //Part.OutputAudioUpdate;
            //Part.RefusalTokenLogProbabilities;
            ret.RefusalUpdate = Part.RefusalUpdate;
            ret.Role = TranslatorRole.TranslateFromProvider(Part.Role);
            //Part.ServiceTier;
            ret.SystemFingerprint = Part.SystemFingerprint;

            //Part.ToolCallUpdates;
            //Part.Usage;
            return ret;

        }
    }


    /// <summary>
    /// Convert <see cref="ButlerChatMessage"/> to OpenAI <see cref="ChatMessage"/> and back
    /// </summary>
    public static class TranslatorChatMessage
    {
        const string UnknownMessageTypeMessage = "Hey somehow a butler chat message part got to the OpenAI translator while set to unknown or not implemented yet value. The translator don't know how to handle that";
        const string NoImageSupportMessage = "Images are currently not supported by the ButlerSDK's OpenAI provider";
        const string NoFileSupportMessage = "File upload to OpenAI currently is not supported by ButlerSDK's OpenAI provider. Note tools are free to dump contents into chat message as needed";
        const string NoAudioSupportMessage = "Audio is not supported yet by ButlerSDK's OpenAI provider";
        static ButlerChatMessageContentPart ContentHandler_ToButler(ChatMessageContentPart x)
        {
            ButlerChatMessageContentPart ret = new();
            switch (x.Kind)
            {
                case ChatMessageContentPartKind.Text:
                    {
                        ret.Text = x.Text;
                        ret.MessageType = ButlerChatMessageType.Text;

                        return ret;
                    }
                case ChatMessageContentPartKind.Refusal:
                    {
                        ret.Refusal = x.Refusal;
                        ret.MessageType = ButlerChatMessageType.Refusal;
                        // DEAR FUTURE CODER: Here's a suspicious thought. We assuming refusal flag is text atm. Do we for DX experience just 
                        // drop the refusal text message into butlerpart.refusal & butlerpart.text? or just leave it null?
                        // Currently I picked for refusal text and normal text getting the same value in that case.
                        // WHY? to let DX just go (OK refusal note: text indicates why)
                        ret.Text = x.Refusal;
                        return ret;
                    }
                default:
                    {
                        throw new NotImplementedException("NON TEXT SOURCE NOT SUPPORTED YET BY ButlerSDK OpenAI provider");
                    }

            }
        }
        static ChatMessageContentPart ContentPartHandler_FromButler(ButlerChatMessageContentPart x)
        {
            ChatMessageContentPart ret;

            switch (x.MessageType)
            {
                case ButlerChatMessageType.Text:
                    {
                        ret = ChatMessageContentPart.CreateTextPart(x.Text);
                        break;
                    }
                case ButlerChatMessageType.Refusal:
                    {
                        ret = ChatMessageContentPart.CreateRefusalPart(x.Refusal);
                        break;
                    }
                case ButlerChatMessageType.Audio:
                    {
                        throw new NotImplementedException(NoAudioSupportMessage);
                    }
                case ButlerChatMessageType.File:
                    {
                        throw new NotImplementedException(NoFileSupportMessage);
                    }
                case ButlerChatMessageType.Image:
                    {
                        throw new NotImplementedException(NoImageSupportMessage);
                    }
                default:
                case ButlerChatMessageType.Unknown: throw new InvalidOperationException(UnknownMessageTypeMessage);
            }
            return ret;
        }



        public static ButlerChatMessage TranslateFromProvider(ChatMessage message)
        {
            /* has unit tests*/
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            ButlerChatMessage ret = new ButlerChatMessage();

            ret.Content = new List<ButlerChatMessageContentPart>();
            foreach (var part in message.Content)
            {
                ret.Content.Add(ContentHandler_ToButler(part));
            }
#pragma warning disable CS8629 // Nullable value type may be null.
            /* justification for this is that the TranslatorFromProvider(Non null and OpenAI object is gonna change it to butler version.
             * Null begets null. True and that don't change here BUT we are using the specified OpenAI role types. */
            if (message is UserChatMessage x)
            {
                ret.Participant = x.ParticipantName;
                ret.Role = (ButlerChatMessageRole)TranslatorRole.TranslateFromProvider(ChatMessageRole.User);
            }
            if (message is AssistantChatMessage assistant)
            {
                ret.Participant = assistant.ParticipantName;
                ret.Role = (ButlerChatMessageRole)TranslatorRole.TranslateFromProvider(ChatMessageRole.Assistant);
            }
            if (message is ToolChatMessage toolchat)
            {
                ret.Id = toolchat.ToolCallId;
                ret.Role = (ButlerChatMessageRole)TranslatorRole.TranslateFromProvider(ChatMessageRole.Tool);
            }

            if (message is SystemChatMessage sys)
            {
                ret.Participant = sys.ParticipantName;

                ret.Role = (ButlerChatMessageRole)TranslatorRole.TranslateFromProvider(ChatMessageRole.System);

            }
#pragma warning restore CS8629 // Nullable value type may be null.            


            return ret;
        }

        public static ChatMessage TranslateToProvider(ButlerChatMessage message)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            List<ChatMessageContentPart> kind = new();
            foreach (var K in message.Content)
            {
#if DEBUG
                Debug.Write($"Converted ");
#endif
                ChatMessageContentPart Translation = ContentPartHandler_FromButler(K);
                kind.Add(Translation);

            }



            switch (message.Role)
            {
                case ButlerChatMessageRole.Assistant:
                    {
                        if (kind.Count != 0)
                            return ChatMessage.CreateAssistantMessage(kind);
                        else
                            return ChatMessage.CreateAssistantMessage(message.Message);
                    }
                case ButlerChatMessageRole.ToolResult:
                    {

                        if (message is not ButlerChatToolCallMessage Conv)
                        {
                            throw new InvalidCastException("Attempt to change a non tool call message into tool call one");
                        }

                        if (message.Message is null)
                        {
                            throw new InvalidDataException($"Error: Attempting to translate a {typeof(ButlerChatToolCallMessage).Name} to OpenAI with a null message.  Be sure to set the message to a string containing the results.");
                        }

                        if (kind.Count != 0)
                        {
                            // TODO: Dear future coder: Consider the question do we throw exception on attempting to translate a tool message without id.
                            var res = ChatMessage.CreateToolMessage(Conv.Id, message.Message);
                            return res;
                        }
                        else
                        {
                            var res = ChatMessage.CreateToolMessage(Conv.Id, message.Message);
                            return res;
                        }

                    }
                case ButlerChatMessageRole.User:
                case ButlerChatMessageRole.None:
                    {
                        if (kind.Count != 0)
                            return ChatMessage.CreateUserMessage(kind);
                        else
                            return ChatMessage.CreateUserMessage(message.Message);
                    }
                case ButlerChatMessageRole.ToolCall:
                    {
                        if (message is not ButlerChatToolCallMessage Conv)
                        {
                            throw new InvalidCastException("Attempt to change a non tool call message into tool call one");
                        }

                        var ToolStuff = new List<ChatToolCall>();
                        string? FuncArgCache = Conv.FunctionArguments;
                        if (FuncArgCache is null)
                        {
                            FuncArgCache = string.Empty;
                        }
                        ToolStuff.Add(ChatToolCall.CreateFunctionToolCall(Conv.Id, Conv.ToolName, BinaryData.FromString(FuncArgCache)));

                        var ret = ChatMessage.CreateAssistantMessage(ToolStuff);
                        return ret;

                    }
                case ButlerChatMessageRole.System:
                    {
                        return ChatMessage.CreateSystemMessage(kind);
                    }
                default:
                    {
                        throw new InvalidOperationException("DEBUG Warning: Attempt to place unrolled chat  message back. Check logic for OpenAI provider in butler");
                    }
            }
        }
    }



    public static class TranslatorStreamingChatToolCalls
    {
        //#error Check how the code that translates the OpenAI provider chat tool call to the butler one.
        public static ButlerStreamingToolCallUpdatePart TranslateFromProvider(StreamingChatToolCallUpdate x)
        {
            string? ToolCallId = null;
            string? FuncName = null;
            string? FuncArgs = null;
            if (x.FunctionArgumentsUpdate is not null)
            {
                FuncArgs = x.FunctionArgumentsUpdate.ToString();
            }

            if (x.FunctionName is not null)
            {
                FuncName = x.FunctionName.ToString();
            }
            if (x.ToolCallId is not null)
            {
                ToolCallId = x.ToolCallId.ToString();
            }


            var ret = new ButlerStreamingToolCallUpdatePart(FuncName, FuncArgs, x.Index, "Function", ToolCallId);


            if (x.FunctionArgumentsUpdate is not null)
                ret.FunctionArgumentsUpdate = x.FunctionArgumentsUpdate.ToString();
            if (x.FunctionName is not null)
                ret.FunctionName = x.FunctionName.ToString();


            ret.Index = x.Index;
            ret.Kind = "Function";

            if (x.ToolCallId is not null)
                ret.ToolCallid = x.ToolCallId.ToString();

            /*            var ret = new ButlerStreamingToolCallUpdatePart();
                        if (x.FunctionArgumentsUpdate is not null)
                            ret.FunctionArgumentsUpdate = x.FunctionArgumentsUpdate.ToString();
                        if (x.FunctionName is not null)
                            ret.FunctionName = x.FunctionName.ToString();


                        ret.Index = x.Index;
                        ret.Kind = "Function";

                        if (x.ToolCallId is not null)
                            ret.ToolCallid = x.ToolCallId.ToString();*/
            //x.FunctionArgumentsUpdate;
            //x.FunctionName;
            //x.Index;
            //x.Kind;
            //x.ToolCallId;
            return ret;
        }
    }

}
