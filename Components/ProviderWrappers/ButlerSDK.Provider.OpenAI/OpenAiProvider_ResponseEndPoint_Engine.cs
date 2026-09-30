using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Providers.OpenAI;
using ButlerToolContract.DataTypes;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Realtime;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
#pragma warning disable OPENAI001
namespace ButlerSDK.Provider.OpenAI
{
    /* the code that is needed for the response is gonna go here and we are essentially



       Entry Provider
           is it RespnseMode or Chat mode.

       If chat mode => the engine is OpenAIProvider located (as well as the rest of the provider)
    If response faking a chat complation => here's here.
     */

    public static class TranslatorStreamingResponseUpdate
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




        public static ButlerStreamingChatCompletionUpdate TranslateFromProvider(StreamingResponseUpdate Part, bool DiscardNulLContentParts = true)
        {
            ButlerStreamingChatCompletionUpdate ret = new();
            bool Gotton = false;
            switch (Part)
            {
                case StreamingResponseIncompleteUpdate StillGoing:
                    {
                        Gotton = true;
                        break;
                    }
                case StreamingResponseFunctionCallArgumentsDeltaUpdate FuncCall:
                    {
                        Gotton = true;
                        break;
                        ButlerStreamingToolCallUpdatePart Pr = new(
                            string.Empty,
                            FuncCall.Delta.ToString(),
                            FuncCall.OutputIndex,
                            "FunctionCall",
                            FuncCall.ItemId);

                        ret.EditableToolCallUpdates.Add(Pr);
                        Gotton = true;
                        break; 
                    }
                case StreamingResponseFunctionCallArgumentsDoneUpdate FuncDone:
                    {
                        Gotton = true;
                        break;
                        string str = FuncDone.FunctionArguments.ToString();
                        if (str is null)
                        {
                            str = "{}";
                        }
                        ButlerStreamingToolCallUpdatePart Pr = new(FuncDone.FunctionName, 
                            str,
                            0,
                            Part.Kind.ToString(),
                            FuncDone.ItemId);
        
                        ret.EditableToolCallUpdates.Add(Pr);
                        Gotton = true;
                        break;
                    }
                case StreamingResponseCreatedUpdate CreatedUpdate:
                    {
                        ret.Model = CreatedUpdate.Response.Model;
                        ret.CreatedAt = CreatedUpdate.Response.CreatedAt;
                        ret.Role = ButlerChatMessageRole.Assistant;
                        ret.Id = CreatedUpdate.Response.Id;
                        
                        Gotton = true;
                        break;
                    }
                case StreamingResponseCompletedUpdate StreamDone:
                {
                        ret.Role = ButlerChatMessageRole.Assistant;
                        ret.Id = StreamDone.Response.Id;
                        ret.CreatedAt = StreamDone.Response.CreatedAt;
                        ret.Id = StreamDone.Response.Id;
                        int ToolCount = 0;
                        if (StreamDone.Response.Status == ResponseStatus.Completed)
                        {
                            ret.FinishReason = ButlerChatFinishReason.Stop;
                        }
                        foreach (var reply in StreamDone.Response.OutputItems)
                        {
                            switch (reply)
                            {
                                case FunctionCallResponseItem CallMe:
                                    {
                                        
                                        {
                                            ButlerStreamingToolCallUpdatePart Pr = new ButlerStreamingToolCallUpdatePart(CallMe.FunctionName, CallMe.FunctionArguments.ToString(), 0, "toolcall", CallMe.Id);
                                            ret.EditableToolCallUpdates.Add(Pr);
                                            ToolCount++;
                                        }
                                        break;
                                    }
                                case MessageResponseItem WriteMe:
                                    {
                                        ButlerChatStreamingPart TextFin = new();
                                        TextFin.Text = string.Empty;
                                        foreach (var TextPart in WriteMe.Content)
                                        {
                                            TextFin.Text += TextPart.Text;
                                        }
                                        break;
                                    }
                            }
                        }
                        if (ToolCount > 0)
                            ret.FinishReason = ButlerChatFinishReason.ToolCalls;
                        Gotton = true;
                    break;
                }
                case StreamingResponseContentPartAddedUpdate ContextDLC:
                    {
                        ret.Model = null;
                        ret.CreatedAt = DateTimeOffset.Now;
                        ret.Role = ButlerChatMessageRole.Assistant;
                        ButlerChatStreamingPart Pr = new ButlerChatStreamingPart();
                        ret.EditorableContentUpdate.Add(Pr);



                        
                        switch (ContextDLC.Part.Kind)
                        {
                            case ResponseContentPartKind.OutputText:
                                Pr.Text = ContextDLC.Part.Text;
                                Pr.Kind = ButlerChatMessagePartKind.Text;
                                break;
                            case ResponseContentPartKind.Refusal:
                                Pr.Text = ContextDLC.Part.Refusal;
                                if (string.IsNullOrEmpty(Pr.Text))
                                {
                                    if (!string.IsNullOrEmpty(ContextDLC.Part.Text))
                                    {
                                        Pr.Text = ContextDLC.Part.Text;
                                    }
                                }
                                Pr.Kind = ButlerChatMessagePartKind.Refusal;
                                break;
                            default:
                                {
                                    throw new NotImplementedException("Unsupported context type");
                                }
                        }
                        Gotton = true;
                        break;
                    }
                case StreamingResponseContentPartDoneUpdate DLCDone:
                    {
                        ret.Model = null;
                        ret.FinishReason = ButlerChatFinishReason.Stop;
                        Gotton = true;
                        break;
                    }
                case StreamingResponseInProgressUpdate StreamUpdate:
                {
                        
                        ret.Model = StreamUpdate.Response.Model;
                        ret.CreatedAt = StreamUpdate.Response.CreatedAt;
                        ret.Role = ButlerChatMessageRole.Assistant;
                        ret.Id = StreamUpdate.Response.Id;
                        /*
                        ButlerChatStreamingPart pr = new();
                        pr.Text = StreamUpdate.Response.GetOutputText();
                        pr.Kind = ButlerChatMessagePartKind.Text;
                        pr.Text = StreamUpdate.Response.GetOutputText();
                        ret.EditorableContentUpdate.Add(pr);*/
                        Gotton = true;
                        //ret = new ButlerStreamingChatCompletionUpdate();
                        break;
                }
                case StreamingResponseOutputItemAddedUpdate OutputUpdate:
                    {
                        Gotton = true;
                        break;
                    }
                case StreamingResponseOutputItemDoneUpdate UpdateDone:
                    {
                        ret.Model = null;
                        Gotton = true;
                        break;
                    }
                case StreamingResponseOutputTextDeltaUpdate TextUpdate:
                    {
                        ButlerChatStreamingPart Pr = new ButlerChatStreamingPart();
                        ret.EditorableContentUpdate.Add(Pr);
                        Pr.Text = TextUpdate.Delta;
                        ret.Role = ButlerChatMessageRole.Assistant;
                        Gotton = true;
                        break;
                    }
                case StreamingResponseOutputTextDoneUpdate TextDone:
                    {
                        ButlerChatStreamingPart Pr = new ButlerChatStreamingPart();
                        ret.EditorableContentUpdate.Add(Pr);
                        Pr.Text = TextDone.Text;
                        ret.FinishReason = ButlerChatFinishReason.Stop;
                        ret.Role = ButlerChatMessageRole.Assistant;
                        Gotton = true;
                        break;
                    }
            }

            if (!Gotton)
                throw new NotImplementedException();
            return ret;
        }
    }



    /// <summary>
    /// This translator maps <see cref="ButlerChatMessageRole"/> to <see cref="ChatMessageRole"/> and back
    /// </summary>
    public static class TranslatorRole
    {
        /* has unit test*/
        public static ButlerChatMessageRole? TranslateFromProvider(ChatMessageRole? x)
        {
            if (x is null)
                return null;
            else
            {
                switch (x)
                {
                    case ChatMessageRole.System:
                        return ButlerChatMessageRole.System;
                    case ChatMessageRole.User:
                        return ButlerChatMessageRole.User;
                    case ChatMessageRole.Assistant:
                        return ButlerChatMessageRole.Assistant;
                    case ChatMessageRole.Tool:
                        return ButlerChatMessageRole.ToolCall;
                    case ChatMessageRole.Function:
                        throw new NotImplementedException("Note: OpenAI depreciated functions in favor of tools. OpenAI provider for ButlerSDK is attempting to attempt function enum conversion. ");
                }
                throw new NotImplementedException("Note: Translator fall-thru for OpenAI ButlerSDK provider enum.  public static ButlerChatMessageRole? TranslateFromProvider(ChatMessageRole? x). Check <- this routine and if extra enum added to the OpenAI.NET sdk");
            }
        }
    }


    public static class TranslatorResponseEntry
    {
        static string fetch_text(ButlerChatMessage msg_data)
        {
            string? ret = null;

            ret = msg_data.GetCombinedText();
            if (ret is null)
            {
                ret = msg_data.Message;
            }
            if (ret == null)
                throw new InvalidDataException("Null message in response mode translation!");
            return ret;
        }
        public static ResponseItem TranslateToProvider(ButlerChatMessage Message)
        {
            ResponseItem ret;
            var msg_data = fetch_text(Message);
            switch (Message.Role)
            {
                case ButlerChatMessageRole.System:
                    {
                        ret = ResponseItem.CreateSystemMessageItem(msg_data);
                        break;
                    }
                case ButlerChatMessageRole.User:
                    {
                        ret = ResponseItem.CreateUserMessageItem(msg_data);
                        break;
                    }
                case ButlerChatMessageRole.Assistant:
                    {
                        ret = ResponseItem.CreateAssistantMessageItem(msg_data);
                        break;
                    }
                case ButlerChatMessageRole.ToolCall:
                    {
                        if (Message is ButlerChatToolCallMessage ToolTimeTry)
                        {
                            if (ToolTimeTry.FunctionArguments == null)
                            {
                                throw new InvalidDataException("Attempt to convert null arguments in response mode. Check for data curropt!");
                            }
                            ret = ResponseItem.CreateFunctionCallItem(ToolTimeTry.Id, ToolTimeTry.ToolName, BinaryData.FromString(ToolTimeTry.FunctionArguments));
                        }
                        else
                        {
                            throw new InvalidDataException("Atetmpt to convert a message to a response tool call... but it's not a tool (ButlerChatToolCallMessage)!");
                        }
                        break;
                    }
                case ButlerChatMessageRole.ToolResult:
                    {
                        if (Message is ButlerChatToolResultMessage ToolOver)
                        {
                            ret = ResponseItem.CreateFunctionCallOutputItem(ToolOver.Id, msg_data);
                        }
                        else
                        {
                            throw new InvalidDataException("Atetmpt to convert a message to a response tool call result but it's not a tool (ButlerChatToolResultMessage)!");
                        }
                        break;
                    }
                case ButlerChatMessageRole.None:
                    {
                        throw new InvalidDataException("Error: Attempt to convert a butler message that does *not* have a role!");
                    }
                default:
                    {
                        throw new InvalidOperationException("Error: unknown enum for the butler chat message role. Ensure matching verions of provider to butler caller");
                    }
            }
            return ret;
        }
    }
    public static class TranslatorChatLogResponse
    {
        public static IList<ResponseItem> TranslateToProvider(IList<ButlerChatMessage> ChatLog)
        {
            ArgumentNullException.ThrowIfNull(ChatLog, nameof(ChatLog));
            List<ResponseItem> ret = new List<ResponseItem>();
            for (int i = 0; i < ChatLog.Count; i++)
            {
                var message = TranslatorResponseEntry.TranslateToProvider(ChatLog[i]);
                ret.Add(message);
            }
            return ret;
        }
    }


    static class TranslatorChatOptions_ResponseOptions
    {
        public static CreateResponseOptions TranslateToProvider(IButlerChatCompletionOptions Ops, IList<ResponseItem> Replies, ResponsesClient ReplyMe, string model, IButlerLLMProvider Handler)
        {
            var ret = new CreateResponseOptions(model, Replies);
            if (ret.InputItems is null)
            {
                throw new InvalidDataException("Error converting response!");
            }
   

            var chat_shim = TranslatorChatOptions.TranslateToProvider(Ops, Handler);


            if (chat_shim.AllowParallelToolCalls is not null)
            {
                ret.ParallelToolCallsEnabled = chat_shim.AllowParallelToolCalls;
            }

            if (chat_shim.AudioOptions is not null)
            {
                throw new NotImplementedException("Audio for repsonse");
            }
            if (chat_shim.EndUserId is not null)
            {
                ret.EndUserId = chat_shim.EndUserId;
            }

            if (chat_shim.FrequencyPenalty is not null)
            {
                throw new NotImplementedException("Frequence penalty don't map to reponse. ");
            }

            if (chat_shim.FunctionChoice is not null)
            {
                throw new NotImplementedException("For butlersdk to openai to response mini provider (tools are still the butler mode!).");
            }

            if (chat_shim.Functions is not null)
            {
                if (chat_shim.Functions.Count is not 0)
                {
                    throw new NotImplementedException("For butlersdk to openai to response mini provider (tools are still the butler mode!).");
                }
            }

            if (chat_shim.IncludeLogProbabilities is not null)
            {
                throw new NotImplementedException("IncludeLog don't map!");
            }

            if (chat_shim.LogitBiases is not null)
            {
                if (chat_shim.LogitBiases.Count is not 0)
                {
                    throw new NotImplementedException("LogitBiases don't map!");
                }
            }

            if (chat_shim.MaxOutputTokenCount is not null)
            {
                ret.MaxOutputTokenCount = chat_shim.MaxOutputTokenCount;
            }

            if (chat_shim.Metadata is not null)
            {
                foreach (var item in chat_shim.Metadata)
                {
                    ret.Metadata.Add(item);
                }
            }

            if (chat_shim.OutputPrediction is not null)
            {
                throw new NotImplementedException("OtuputPriction don't map");
            }

            if (chat_shim.PresencePenalty is not null)
            {
                throw new NotImplementedException("PresencePenalty don't map");
            }
            ret.ReasoningOptions = new ResponseReasoningOptions();
            if (chat_shim.ReasoningEffortLevel is not null)
            {
                if (chat_shim.ReasoningEffortLevel == ChatReasoningEffortLevel.High)
                {

                    ret.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.High;
                }
                else if (chat_shim.ReasoningEffortLevel == ChatReasoningEffortLevel.Low)
                {
                    ret.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Low;
                }
                else if (chat_shim.ReasoningEffortLevel == ChatReasoningEffortLevel.Medium)
                {
                    ret.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium;
                }
                else if (chat_shim.ReasoningEffortLevel == ChatReasoningEffortLevel.Minimal)
                {
                    ret.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Minimal;
                }
                else if (chat_shim.ReasoningEffortLevel == ChatReasoningEffortLevel.None)
                {
                    ret.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.None;
                }
                else
                {
                    throw new NotImplementedException("the chat reponse deasoning effector don't match a reponse resoning effort");
                }
            }

            if (chat_shim.ResponseFormat is not null)
            {
                throw new NotImplementedException("Responseformat not supported");
            }

            if (chat_shim.ResponseModalities != ChatResponseModalities.Default)
            {
                throw new NotImplementedException("text only supported!");
            }

            if (chat_shim.SafetyIdentifier is not null)
            {
                ret.SafetyIdentifier = chat_shim.SafetyIdentifier;
            }

            if (chat_shim.Seed is not null)
            {
                throw new NotImplementedException("SEED NOT supported!");
            }

            if (chat_shim.ServiceTier is not null)
            {
                if (chat_shim.ServiceTier == ChatServiceTier.Auto)
                {
                    ret.ServiceTier = ResponseServiceTier.Auto;
                }
                else if (chat_shim.ServiceTier == ChatServiceTier.Default)
                {
                    ret.ServiceTier = ResponseServiceTier.Default;
                }
                else if (chat_shim.ServiceTier == ChatServiceTier.Scale)
                {
                    ret.ServiceTier = ResponseServiceTier.Scale;
                }
                else if (chat_shim.ServiceTier == ChatServiceTier.Flex)
                {
                    ret.ServiceTier = ResponseServiceTier.Flex;
                }
                else
                {
                    throw new NotImplementedException("No service matching teier");
                }
            }

            if (chat_shim.StopSequences is not null)
            {
                if (chat_shim.StopSequences.Count is not 0)
                {
                    throw new NotImplementedException("StopSequences don't reponsemode;");
                }
            }

            if (chat_shim.StoredOutputEnabled is not null)
            {
                ret.StoredOutputEnabled = chat_shim.StoredOutputEnabled;
            }

            if (chat_shim.Temperature is not null)
            {
                ret.Temperature = chat_shim.Temperature;
            }

            if (chat_shim.Tools is not null)
            {
                if (chat_shim.Tools.Count != 0)
                {
                    ret.Tools.Clear();
                    foreach (ChatTool x in chat_shim.Tools)
                    {
                        BinaryData FinaleArgs = x.FunctionParameters;
                        string add_reponse_check = x.FunctionParameters.ToString();
                        if (!add_reponse_check.Contains("additionalProperties"))
                        {
                            var TmpParse = JsonNode.Parse(add_reponse_check);
                            if (TmpParse is not null)
                            {
                                TmpParse["additionalProperties"] = false;
                            }
                            else
                            {
                                throw new InvalidDataException("Crit error: Reponse Openai branch required argument additionalProperties and unable to poke it there before trnslating");
                            }
                            FinaleArgs = BinaryData.FromString(TmpParse.ToString());
                        }
                        ResponseTool ReplyTool = ResponseTool.CreateFunctionTool(x.FunctionName, FinaleArgs, true, x.FunctionDescription);
                        ret.Tools.Add(ReplyTool);
                    }
                    if (chat_shim.ToolChoice is not null)
                    {
                        var r = ChatToolChoice.CreateRequiredChoice();
                        if (Ops.ToolChoice is not null)
                        {
                            switch (Ops.ToolChoice)
                            {
                                case ButlerChatToolChoice.None:
                                    ret.ToolChoice = ResponseToolChoice.CreateNoneChoice();
                                    break;
                                case ButlerChatToolChoice.Required:
                                    ret.ToolChoice = ResponseToolChoice.CreateRequiredChoice();
                                    break;
                                case ButlerChatToolChoice.Auto:
                                    ret.ToolChoice = ResponseToolChoice.CreateAutoChoice();
                                    break;
                                default:
                                    throw new NotImplementedException("Hey this mode for tools not mapping to reponse");
                            }
                        }
                    }
                }
                if (chat_shim.TopLogProbabilityCount is not null)
                {
                    ret.TopLogProbabilityCount = chat_shim.TopLogProbabilityCount;
                }

                if (chat_shim.TopP is not null)
                {
                    ret.TopP = chat_shim.TopP;
                }

                if (chat_shim.WebSearchOptions is not null)
                {
                    throw new NotImplementedException("Web search via openai not supported");
                }


            }
            return ret;
        }
    }

    public class ButlerOpenAiResponseClient : IButlerChatClient
    {
        ResponsesClient MyClient;
        string model;
        IButlerLLMProvider ProviderSource;
        IButlerChatPreprocessor? PPR = null;

        internal ButlerOpenAiResponseClient(OpenAIClient x, string Model, IButlerLLMProvider Source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Model);
            ArgumentNullException.ThrowIfNull(x);
            MyClient = x.GetResponsesClient();
            this.ProviderSource = Source;
            this.PPR = DefaultOpenAiProvider.Instance;
            this.model = Model;
        }

        internal ButlerOpenAiResponseClient(ResponsesClient myClient, IButlerLLMProvider Source, IButlerChatPreprocessor? PPR, string Model)
        {

            ArgumentNullException.ThrowIfNull(Source);
            ArgumentNullException.ThrowIfNull(myClient);
            MyClient = myClient;
            this.ProviderSource = Source;
            this.PPR = PPR;
            this.model = Model;
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

            IList<ResponseItem>
            NewProtocol = TranslatorChatLogResponse.TranslateToProvider(PPRMSG);


            CreateResponseOptions Opts = new CreateResponseOptions(model, NewProtocol);


            var result = MyClient.CreateResponse(Opts);

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

            //List<ChatMessage> ProviderFormat = (List<ChatMessage>)TranslatorChatLog.TranslateToProvider(PPRMSG);
            IList<ResponseItem>
NewProtocol = TranslatorChatLogResponse.TranslateToProvider(PPRMSG);


            //ChatCompletionOptions ProviderOptions = TranslatorChatOptions.TranslateToProvider(options, ProviderSource);

            CreateResponseOptions Opts = TranslatorChatOptions_ResponseOptions.TranslateToProvider(options, NewProtocol, MyClient, this.model, this.ProviderSource);
            if (Opts.Tools is not null)
            {
                if ((Opts.Tools.Count > 0))
                {
                    if (Opts.ToolChoice is null)
                        Opts.ToolChoice = ResponseToolChoice.CreateAutoChoice();
                }
                else
                {
                    //Opts.ToolChoice = null;
                  }
            }
            Opts.StreamingEnabled = true;
            var Response = this.MyClient.CreateResponseStreamingAsync(Opts);


            //await foreach (var part in Result.WithCancellation(cancelMe))
            await foreach (var part in Response.WithCancellation(cancelMe))
            {
                if (part is not null)
                {
                    var butlerpart = TranslatorStreamingResponseUpdate.TranslateFromProvider(part);
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
            throw new NotImplementedException();
            //return new ButlerOpenAiCollectionResult<ButlerStreamingChatCompletionUpdate>(Result);
        }
    }
}
#pragma warning restore OPENAI001