using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using SecureStringHelper;
using System.ClientModel;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

/*
 * The provider for OpenAI for butlerr5.
 * 
 * TODO: until 100% coverage here in unit tests (the OpenAiProvider) unit test class) 
 * as each thing gets tests, add note that it has unit tests.
 */
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenAiProvider")]
namespace ButlerSDK.Providers.OpenAI
{

  

    /// <summary>
    /// This class is responsible for Translating ChatOptions to the butler one and back
    /// </summary>
    public static class TranslatorChatOptions
    {
        /// <summary>
        /// Make an instance of the OpenAI chat tool class from this this.
        /// </summary>
        /// <param name="baseInterface">the butler interface</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ChatTool DefaultToolCode(IButlerToolBaseInterface baseInterface)
        {
            ArgumentNullException.ThrowIfNull(baseInterface, nameof(baseInterface));    
            var str = baseInterface.GetToolJsonString();
            return ChatTool.CreateFunctionTool(baseInterface.ToolName, baseInterface.ToolDescription, BinaryData.FromString(str));
        }

        /// <summary>
        /// Blank output and convert the passed list of input into the format output expands
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void SeedToolConversionToProvider(IList<IButlerToolBaseInterface> input, IList<ChatTool> output, bool DoNotResetOutput=false)
        {
            if (!DoNotResetOutput)
                output.Clear();
            foreach (IButlerToolBaseInterface inputItem in input)
            {
                output.Add(DefaultToolCode(inputItem));
            }
        }

        

        

        public static ButlerChatCompletionOptions TranslateFromProvider(ChatCompletionOptions X)
        {
            var ret = new ButlerChatCompletionOptions();
            // Let's assume 'ret' is an instance of OpenAI.Chat.ChatCompletionOptions
            // and 'X' is your IButlerChatCompletionOptions instance.

            ret.AllowParallelToolCalls = X.AllowParallelToolCalls;
            ret.EndUserId = X.EndUserId;
            ret.PresencePenalty = X.PresencePenalty;
            ret.FrequencyPenalty = X.FrequencyPenalty;

            //ret.IncludeLogProbabilities = X.IncludeLogProbabilities;
            //ret.LogitBiases = X.LogitBiases;
            ret.MaxOutputTokenCount = X.MaxOutputTokenCount;
            //ret.Metadata = X.Metadata; // This is read-only in the OpenAI object. Correct to omit.
            //ret.OutputPrediction = 
            ret.PresencePenalty = X.PresencePenalty;
            //ret.ReasoningEffortLevel = X.ReasoningEffortLevel;
            //ret.ResponseFormat = X.ResponseFormat;
            //ret.Seed = X.Seed;
            //ret.ServiceTier = X.ServiceTier;
            //ret.StopSequences = X.StopSequences;
            //ret.StoredOutputEnabled = X.StoredOutputEnabled;
            ret.Temperature = X.Temperature;

            var Nonce = ChatToolChoice.CreateNoneChoice();
            var Auto = ChatToolChoice.CreateAutoChoice();
            var Required = ChatToolChoice.CreateRequiredChoice();

            if (X.ToolChoice == Nonce)
            {
                ret.ToolChoice = ButlerChatToolChoice.None;
            } else if (X.ToolChoice == Auto)
            {
                ret.ToolChoice = ButlerChatToolChoice.Auto;
            } else if (X.ToolChoice == Required)
            {
                ret.ToolChoice = ButlerChatToolChoice.Required;
            } else if (X.ToolChoice is null)
            {
                ret.ToolChoice = null; 
            }
            else
            {
                throw new InvalidOperationException("Unexpected tool choice in the filter between Butler's tool collection and OpeAI's once. Check TranslatorChatOption.  public static ButlerChatCompletionOptions TranslateFromProvider(ChatCompletionOptions X)");
            }
   

            //SeedToolConversion(X.Tools, ret.Tools);
            // the code works BUT the comment is here to warn future me/users
            // This line below is dangerous. You're clearing the list after passing it to the conversion method.
            // Ensure SeedToolConversion *adds* to ret.Tools, it doesn't just take a reference that you then clear.
            // A better pattern would be: `var providerTools = SeedToolConversion(X.Tools); foreach(var t in providerTools) { ret.Tools.Add(t); }`
            ret.Tools.Clear();


            //ret.TopLogProbabilityCount = X.TopLogProbabilityCount;
            ret.TopP = X.TopP;
            //ret.WebSearchOptions =
            return ret;
 
        }
        public static ChatCompletionOptions TranslateToProvider(IButlerChatCompletionOptions Opts, IButlerLLMProvider ConversionSource)
        {
            var ret = new ChatCompletionOptions();
            ret.AllowParallelToolCalls = Opts.AllowParallelToolCalls;
            ret.EndUserId = Opts.EndUserId;
            if (Opts.PresencePenalty is not null)
            {
                ret.PresencePenalty = (float)Opts.PresencePenalty;
            }
            if (Opts.FrequencyPenalty is not null)
            { ret.FrequencyPenalty = (float)Opts.FrequencyPenalty;
            }


      


            //ret.IncludeLogProbabilities = Opts.IncludeLogProbabilities;
            // ret.LogitBiases = Opts.LogitBiases;
            ret.MaxOutputTokenCount = Opts.MaxOutputTokenCount;
            //ret.Metadata = Opts.Metadata; <_ readonly
            //ret.OutputPrediction = 
            if (Opts.PresencePenalty is not null)
                ret.PresencePenalty = (float)Opts.PresencePenalty;
            //ret.ReasoningEffortLevel = Opts.ReasoningEffortLevel;
            //ret.ResponseFormat = Opts.ResponseFormat;
            //ret.Seed = Opts.Seed;
            //ret.ServiceTier = Opts.ServiceTier;
            //ret.StopSequences = Opts.StopSequences;
            //ret.StoredOutputEnabled = Opts.StoredOutputEnabled;
            if (Opts.Temperature is not null)
             ret.Temperature = (float)Opts.Temperature;

            switch (Opts.ToolChoice)
            {
                case ButlerChatToolChoice.None: ret.ToolChoice = ChatToolChoice.CreateNoneChoice(); break;
                case ButlerChatToolChoice.Auto: ret.ToolChoice = ChatToolChoice.CreateAutoChoice(); break;
                case ButlerChatToolChoice.Required: ret.ToolChoice = ChatToolChoice.CreateRequiredChoice(); break;
            }

            SeedToolConversionToProvider(Opts.Tools, ret.Tools);
            //ret.Tools.Clear();


            //ret.TopLogProbabilityCount = Opts.TopLogProbabilityCount;
            if (Opts.TopP is not null)
            {
                ret.TopP = (float)Opts.TopP;
            }
            //ret.WebSearchOptions =
            if (ConversionSource is null)
            {
                if (Opts.Tools.Count > 0)
                {
                    throw new InvalidOperationException("Hey, a call to translate Butler's generic tool system to the OpenAI provider one went thru incorrect without a conversion for tools. public static ChatCompletionOptions TranslateToProvider(IButlerChatCompletionOptions Opts, ->IButlerLLMProvider ConversionSource<-)");
                }
            }
            else
            {
                foreach (var ToolInterface in Opts.Tools)
                {
                    object whatbox = ConversionSource.CreateChatTool(ToolInterface);
                    ChatTool WhatsIn = (ChatTool)whatbox;
                    if (WhatsIn is not null)
                    {
                        ret.Tools.Add(WhatsIn);
                    }
                    else
                    {
                        throw new InvalidOperationException("Hey a CreateChatTool for an OpenAI based provider failed to actual return correct data type - ChatTool");
                    }
                }
            }
            return ret;

            /*
             * DIRECTIONS for translating:
             * When the datatype is added below add a -> next to it.
            Opts.AllowParallelToolCalls;
            Opts.AudioOptions;
            Opts.EndUserId;
            Opts.FrequencyPenalty;
            Opts.FunctionChoice;
            Opts.Functions;
            Opts.IncludeLogProbabilities;
            Opts.LogitBiases;
            Opts.MaxOutputTokenCount;
            Opts.Metadata;
            Opts.OutputPrediction;
            Opts.PresencePenalty;
            Opts.ReasoningEffortLevel;
            Opts.ResponseFormat;
            Opts.ResponseModalities;
            Opts.Seed;
            Opts.ServiceTier;
            Opts.StopSequences;
            Opts.StoredOutputEnabled;
            Opts.Temperature;
            Opts.ToolChoice;
            Opts.Tools;
            Opts.TopLogProbabilityCount;
            Opts.TopP;
            Opts.WebSearchOptions; */

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
    /// <summary>
    /// Go From <see cref="ChatFinishReason"/> to <see cref="ButlerChatFinishReason"/>
    /// </summary>
    public static class TranslatorFinishReason
    {
        /* has unit test*/
        public static ButlerChatFinishReason? TranslateFromProvider(ChatFinishReason? x)
        {
            switch (x)
            {
                case ChatFinishReason.Stop:
                    return ButlerChatFinishReason.Stop;
                case ChatFinishReason.Length:
                    return ButlerChatFinishReason.Length;
                case ChatFinishReason.ContentFilter:
                    return ButlerChatFinishReason.ContentFilter;
                case ChatFinishReason.ToolCalls:
                    return ButlerChatFinishReason.ToolCalls;
                case ChatFinishReason.FunctionCall:
                    return ButlerChatFinishReason.FunctionCall;
            }
            if (x is null)
            {
                return null;
            }
            throw new NotImplementedException("Gonna want to check if OpenAI added extra enum to ChatFinishReason and ensure TranslatorFinishReason accounts.");
        }
    }
    public static class TranslatorStreamingChatToolCalls
    {
        //#error Check how the code that translates the OpenAI provider chat tool call to the butler one.
        public static ButlerStreamingToolCallUpdatePart TranslateFromProvider(StreamingChatToolCallUpdate x)
        {
            string? ToolCallId=null;
            string? FuncName=null;
            string? FuncArgs=null;
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
                default:  Debugger.Break(); break;

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


        
        
        public static ButlerStreamingChatCompletionUpdate TranslateFromProvider(StreamingChatCompletionUpdate Part, bool DiscardNulLContentParts=true)
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
                ret.Role = (ButlerChatMessageRole) ButlerSDK.Providers.OpenAI.TranslatorRole.TranslateFromProvider(ChatMessageRole.User);
            }
            if (message is AssistantChatMessage assistant)
            {
                ret.Participant = assistant.ParticipantName;
                ret.Role = (ButlerChatMessageRole)ButlerSDK.Providers.OpenAI.TranslatorRole.TranslateFromProvider(ChatMessageRole.Assistant);
            }
            if (message is ToolChatMessage toolchat) 
            {
                ret.Id = toolchat.ToolCallId;
                ret.Role = (ButlerChatMessageRole)ButlerSDK.Providers.OpenAI.TranslatorRole.TranslateFromProvider(ChatMessageRole.Tool);
            }

            if (message is SystemChatMessage sys)
            {
                ret.Participant = sys.ParticipantName;

                ret.Role = (ButlerChatMessageRole)ButlerSDK.Providers.OpenAI.TranslatorRole.TranslateFromProvider(ChatMessageRole.System);

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

                        
                        if (kind.Count != 0)
                        {
                            // TODO: Dear future coder: Consider the question do we throw exception on attempting to translate a tool message without id.
                            var res = ChatMessage.CreateToolMessage(Conv.Id, message.Message);
                            return res;
                        }
                        else
                        {
                            var res = ChatMessage.CreateToolMessage(Conv.Id , message.Message);
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

    public class OpenAiSupportedModelList: IButlerChatCreationSupportedModels
    {
               IList<string> Models;
        public OpenAiSupportedModelList(List<string> models)
        {
            Models = models;
        }
        public IEnumerable<string> GetEnumerator()
        {
            return (IEnumerable<string>)Models.GetEnumerator();
        }

    }

    /// <summary>
    /// Convert a list of <see cref="ButlerChatMessage"/> to <see cref="ChatMessage"/> and back
    /// </summary>
    public static class TranslatorChatLog
    {

        public static IList<ChatMessage> TranslateToProvider(IList<ButlerChatMessage> ChatLog)
        {
            ArgumentNullException.ThrowIfNull(ChatLog, nameof(ChatLog));
            List<ChatMessage> ret = new List<ChatMessage>();
            for (int i = 0; i < ChatLog.Count; i ++)
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
                if (y is ButlerChatToolCallMessage )
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

    /// <summary>
    /// Convert the open ai model list requested to a series of strings
    /// </summary>
    public class ButlerOpenAiModelList : IButlerChatCreationSupportedModels
    {
        OpenAIModelCollection ReadThis;
        public ButlerOpenAiModelList(OpenAIModelCollection Colleciton)
        {
            ArgumentNullException.ThrowIfNull(Colleciton);
            ReadThis = Colleciton;
        }
        public IEnumerable<string> GetEnumerator()
        {
            foreach (var Model in ReadThis)
            {
                yield return Model.Id;
            }
        }
    }

    

    class LoggerClass : ILoggerFactory
            {
        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }
        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    class ProviderLogger : ILogger<ButlerOpenAiProvider>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            throw new NotImplementedException();
        }
    }
    public class ButlerOpenAiProvider : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProvider_RecoverOptions
    {
        const string notInitializedYet = "Not initialized yet with APIKEY. Please call Initialize() or go here for model list. The strings to pick into what model to pick from are there too. https://platform.openai.com/docs/models";
        /// <summary>
        /// our personal log stream
        /// </summary>
        ILogger<ButlerOpenAiProvider>? _factory;



        /// <summary>
        /// This is the endpoint we use if NOT default
        /// </summary>
        Uri? ChangedEndPoint;

        /// <summary>
        /// the OpenAI thing that we're actually wrapping
        /// </summary>
        OpenAIClient? OpenAIHandler;

      
        public ButlerOpenAiProvider(ILogger<ButlerOpenAiProvider>? Logging=null,ILoggerFactory? LogFactory=null): this(null, Logging, LogFactory)
        {
           
        }

        public ButlerOpenAiProvider(Uri? EndPoint, ILogger<IButlerLLMProvider>? Logging = null, ILoggerFactory? LogFactory = null)
        {
            ChangedEndPoint = EndPoint;
            // we don't actually initialize the OpenAI client until Initialize() is called.
            if ((Logging is null) && (LogFactory is not null))
            {
                this._factory = LogFactory.CreateLogger<ButlerOpenAiProvider>();
            }

            if (EndPoint is not null)
            {
                this._factory?.LogInformation("Created {\"IButlerLLMProvider\"} as a Provider with altered Endpoint {\"end\"}.", nameof(ButlerOpenAiProvider), EndPoint.ToString());
            }
            else
            {
                this._factory?.LogInformation("Created {\"IButlerLLMProvider\"} as a Provider and using Default Endpoint", nameof(ButlerOpenAiProvider));
            }
        }

        
        public IButlerChatCompletionOptions DefaultOptions
        {
            get
            {
                this._factory?.LogInformation("Retrieved default chat completion options for provider {provider}.", nameof(ButlerOpenAiProvider));
                return TranslatorChatOptions.TranslateFromProvider(new ChatCompletionOptions());
            }
        }

        public IButlerChatCreationProvider ChatCreationProvider
        {
            get
            {
                this._factory?.LogInformation("Services requested as chat creation provide for {provider}. Note the main provider will handle this.",  nameof(ButlerOpenAiProvider) );
                return this as IButlerChatCreationProvider;
            }
        }

        /// <summary>
        /// Returns the list of supported models for this provider. If you have not initialized with an API key yet this will throw an exception.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>"
        public IButlerChatCreationSupportedModels? SupportedModels
        {
            get
            {
                if (OpenAIHandler is not null)
                {
                    this._factory?.LogInformation("Model Enum Service Requested for {provider}.", nameof(ButlerOpenAiProvider));
                    try
                    {
                        var Models = OpenAIHandler.GetOpenAIModelClient().GetModels();
                        return new ButlerOpenAiModelList(Models);
                    }
                    catch (Exception ex)
                    {
                        this._factory?.LogError(ex, "Error retrieving model list from OpenAI for provider {provider}.", nameof(ButlerOpenAiProvider));
                        throw;
                    }
                }
                else
                {

                    this._factory?.LogError(notInitializedYet, Array.Empty<object>());
                    throw new NotImplementedException(notInitializedYet);
                }

            }
        }

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            if (butlerToolBase == null)
            {
                this._factory?.LogError("Null tool interface passed to CreateChatTool in {provider}.", nameof(ButlerOpenAiProvider));
            }
            ArgumentNullException.ThrowIfNull(butlerToolBase, nameof(butlerToolBase));
            return TranslatorChatOptions.DefaultToolCode(butlerToolBase);
        }

        /// <summary>
        /// Get a client for this provider for asking for chat completions.
        /// </summary>
        /// <param name="model">name of the model to get</param>
        /// <param name="Options">Note for this provider "OpenAi" It does not use the options parameters.</param>
        /// <param name="PPR">This essentially will let you modify what's sent to open ai. It's an extra step BUT optional. It's recommended no unless you need. THE PPR gets the message list, needs to make a new list and return *that* copy</param>
        /// <returns></returns>
        /// <exception cref="ModuleNotFoundException">If the provider can't supply a chat for that model, this exception triggers</exception>
        /// <exception cref="InvalidOperationException">If the <see cref="Initialize(SecureString)"/> has not successfully gotten the underlying OpenAI object, this will be thrown </exception>
        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            if (PPR is null)
                this._factory?.LogInformation("Chat client request for model {model} in provider {provider}. with no pre processor (PPR)", model, nameof(ButlerOpenAiProvider));
            else
                this._factory?.LogInformation("Chat client request for model {model} in provider {provider} with pre processor (PPR) of type {PPRTYPE}.", model, nameof(ButlerOpenAiProvider), PPR.GetType().Name);

            if (OpenAIHandler is null)
            {
                this._factory?.LogError(notInitializedYet, Array.Empty<object>());
                throw new InvalidOperationException(notInitializedYet);
            }
            ChatClient? client = OpenAIHandler.GetChatClient(model);
            if (client is null)
            {
                this._factory?.LogError("{Provider} could not find model {model} to create chat client.",  nameof(ButlerOpenAiProvider), model );
                throw new ModuleNotFoundException(model);
            }
            return new ButlerOpenAiChatClient(client,this, PPR);
        }

        public void Initialize(SecureString key)
        {
            if (key is null)
            {
                this._factory?.LogError("Null API key passed to Initialize() in {provider}.", nameof(ButlerOpenAiProvider) );
            }
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            if (OpenAIHandler is null)
            {
                /* our choice is essentially if endpoint is null use default OpenAI endpoint otherwise use custom one */
                if (this.ChangedEndPoint is null)
                {
                    OpenAIHandler = new OpenAIClient(key.DecryptString());
                    _factory?.LogInformation("Initialized OpenAIClient with default endpoint in {provider}.", nameof(ButlerOpenAiProvider));
                }
                else
                {
                    var opts = new OpenAIClientOptions()
                    {
                        Endpoint = ChangedEndPoint,
                    };

                    OpenAIHandler = new OpenAIClient(new ApiKeyCredential(key.DecryptString()), opts);
                    _factory?.LogInformation("Initialized OpenAIClient with custom endpoint {end} in {provider}.", ChangedEndPoint.ToString(), nameof(ButlerOpenAiProvider));
                }
            }
            if (OpenAIHandler is null)
            {
                
                _factory?.LogError("Failed to Initialize OpenAI required class in {provider}.", nameof(ButlerOpenAiProvider));
                throw new InvalidOperationException("Failed to Initialize OpenAI required class");
            }
            
        }

        public bool StreamingErrorHandler(Exception x, bool IsAggreated, out int SleepTime)
        {
            SleepTime = 0;
            return false;
        }
    }
    /// <summary>
    /// Implements <see cref="IButlerClientResult"/> without directly exposing <see cref="ClientResult"/> to butler;
    /// </summary>
    public class ButlerOpenAiClientResult : IButlerClientResult
    {
        ClientResult<ChatCompletion> ClientResult;
        public ButlerOpenAiClientResult(ClientResult<ChatCompletion> ClientResult)
        {
            ArgumentNullException.ThrowIfNull(nameof(ClientResult));
            this.ClientResult = ClientResult;
        }
        /// <summary>
        /// Gets the contents of the client result as an array of bytes
        /// </summary>
        /// <returns></returns>
        public byte[]? GetBytes()
        {
            return ClientResult.GetRawResponse().Content.ToArray();
        }

        /// <summary>
        /// Gets the client result as a string
        /// </summary>
        /// <returns></returns>
        public string? GetResult()
        {
            
            return ClientResult.ToString();
        }

        public ButlerClientResultType GetResultType()
        {
            return ButlerClientResultType.String;
        }
    }

    public class ButlerOpenAiAsyncCollectionResult: IButlerAsynchCollectionResult<ButlerStreamingChatCompletionUpdate>
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

        public async IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, [EnumeratorCancellation] CancellationToken cancelMe=default)
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
}
