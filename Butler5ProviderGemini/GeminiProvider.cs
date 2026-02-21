using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using GenerativeAI.Types;
using SecureStringHelper;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ButlerSDK.Providers.Gemini
{

    internal static class DebugMode
    {
        static readonly JsonSerializerOptions Mode = new JsonSerializerOptions(JsonSerializerDefaults.General);
        static void Write(Stream target, string data)
        {
            var Output = Encoding.UTF8.GetBytes(data);
            target.Write(Output);
        }
        static void WriteLine(Stream stream, string data)
        {
            Write(stream, data + "\r\n");
        }
        public static void TriggerVerboseOutput(Stream? ButlerStream, Stream? GeminiStream, IList<ButlerChatMessage> msg, GenerateContentRequest GMS)
        {
            if (ButlerStream is not null)
            {
                WriteLine(ButlerStream, "Butler Protocol:");
                for (int i = 0; i < msg.Count; i++)
                {
                    WriteLine(ButlerStream, ($"Message {i}:"));
                    WriteLine(ButlerStream, JsonSerializer.Serialize(msg[i], Mode));
                }
            }

            if (GeminiStream is not null)
            {
                WriteLine(GeminiStream, ("Gemini Translation"));
                foreach (Content con in GMS.Contents)
                {
                    WriteLine(GeminiStream, (JsonSerializer.Serialize(con)));
                }
            }
        }
    }

    [Flags]
    public enum FlagMode
    {
        Off = 0,
        /// <summary>
        /// This means the <see cref="ButlerChatMessage"/> and <see cref="GenerateContentRequest"/> will be echoed to a set stream
        /// </summary>
        WantBeforeAfter = 1,
    }
#if DEBUG
    /// <summary>
    /// This class is to help debug Gemini translator. It
    /// </summary>
    public static class DebugSettings
    {
        static FlagMode Mode= FlagMode.Off;

        static Stream? SetTranslatorVerboseOutputMode_Butler=null;
        static Stream? SetTranslatorVerboseOutputMode_Gemini=null;

        public static void VerboseLogHandler(IList<ButlerChatMessage> msg, GenerateContentRequest GMS)
        {
            if (Mode.HasFlag(FlagMode.WantBeforeAfter))
            {
                DebugMode.TriggerVerboseOutput(SetTranslatorVerboseOutputMode_Butler, SetTranslatorVerboseOutputMode_Gemini, msg, GMS);
            }
        }
        public static void SetTranslatorVerboseOutputMode(Stream ButlerEachRequest, Stream GeminiEchoRequest)
        {
            Mode |= FlagMode.WantBeforeAfter;
            DebugSettings.SetTranslatorVerboseOutputMode_Butler = ButlerEachRequest;
            DebugSettings.SetTranslatorVerboseOutputMode_Gemini = GeminiEchoRequest;
        }
    }
#else
public static class DebugSettings
    {


        public static void VerboseLogHandler(IList<ButlerChatMessage> msg, GenerateContentRequest GMS)
        {
           
        }
        public static void SetTranslatorVerboseOutputMode(Stream ButlerEachRequest, Stream GeminiEchoRequest)
        {

        }
    }
#endif
    public class ButlerGeminiProvider : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProviderToolRequests
    {
        GenerativeAI.GoogleAi? api;
        /* has unit tests*/
        public  ButlerGeminiProvider()
        {
            
        }
        public IButlerChatCreationProvider ChatCreationProvider
        {
            get
            {
                return this as IButlerChatCreationProvider;
            }
        }

        public IButlerChatCreationSupportedModels? SupportedModels
        {
            get
            {
                if (api is null)
                {
                    throw new InvalidOperationException("Google Gemini Provider not initialized. Do that first.");
                }
                else
                {
                    var Models = api.ListModelsAsync();
                    return new GeminiSupportedModels(Models);
                }
            }
        }

        public IButlerChatCompletionOptions DefaultOptions
        {
            get
            {
               return TranslatorChatCompletionObjects.TranslateFromProvider(new GenerationConfig());
            }
        }

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            FunctionDeclaration func = new FunctionDeclaration();
            func.Name = butlerToolBase.ToolName;
            //func.Behavior = Behavior.BLOCKING; // Gemini Provider supports blocking only for now as a description.
            func.Behavior = Behavior.UNSPECIFIED; ; 
            func.Description = butlerToolBase.ToolDescription;

            JsonDocument? doc;
            try
            {
                doc = JsonDocument.Parse(butlerToolBase.GetToolJsonString());
                func.ParametersJsonSchema = doc.RootElement.AsNode();
            }
            catch (JsonException)
            {
                // temp 
                var schemaDict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(butlerToolBase.GetToolJsonString());
                func.ParametersJsonSchema = JsonSerializer.Serialize( schemaDict1);
                func.Parameters = null;
            }

            return func;
        }

        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            if (api is null)
            {
                throw new InvalidOperationException("Google Gemini Provider not initialized. Do that first.");
            }

            var provider_client = api.CreateGenerativeModel(model);
            provider_client.FunctionCallingBehaviour = new GenerativeAI.Core.FunctionCallingBehaviour()
            {
                AutoCallFunction = false
            };

            IGenerativeModel? gen_model = new Gemini.GenericModelForward(provider_client);
            if (gen_model is null)
            {
                throw new InvalidOperationException("Failed to create Gemini chat client for model " + model);
            }
            return new ButlerGeminiChatClient(gen_model, this, PPR);
        }

        public IButlerLLMProvider.ToolProviderCallBehavior GetToolMode()
        {
            return IButlerLLMProvider.ToolProviderCallBehavior.OneShot;
        }

        public void Initialize(SecureString x)
        {
            ArgumentNullException.ThrowIfNull(x);
            if (x.Length == 0)
            {
                throw new ArgumentException("Google Gemini Provider needs non empty key. Go set that API at https://aistudio.google.com/ and DO NOT hard code it in you source code if using source control (i.e. GitHub)");
            }
                    
            if (api is null)
            {
                api = new GoogleAi(x.DecryptString());
            }

            if (api is null)
            {
                throw new InvalidOperationException("Failed to initialize Google Gemini Provider");
            }
        }


    }


    public class GeminiSupportedModels : IButlerChatCreationSupportedModels
    {
        /* technically has matching unit test class but we need actual internet to test it. So unit testing is blank for now*/
        Task<ListModelsResponse> Models;
        public GeminiSupportedModels(Task<ListModelsResponse> models)
        {
            ArgumentNullException.ThrowIfNull(models);
            Models = models;
        }
        public IEnumerable<string> GetEnumerator()
        {
            var res = Models.Result;
            if (res is not null)
            {
                var models = res.Models;
                if (models is null)
                {
                    throw new InvalidOperationException("Unable to fetch Google Gemini data to enumerate models");
                }
                if (models is not null)
                {
                    foreach (var model in models)
                    {
                        yield return model.Name;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Unable to fetch Google Gemini data to enumerate models");
            }
        }
    }

    public static class GeminiAssist_ThoughtSigHelper
    {
        /// <summary>
        /// This maps to the thought signification that Gemini wants for content. It gets placed in <see cref="ButlerChatMessageContentPart.ProviderSpecific"/> if used. Should not *not* need Gemini specific, leave it alone
        /// </summary>
        public const string GeminiThoughSigKey = "GenModelThinking";

        internal static void GeminiToButlerThoughtStore(Part part, ButlerChatStreamingPart chatPart)
        {
            if (part.ThoughtSignature is not null)
            {
                chatPart.ProviderSpecfic[GeminiThoughSigKey] = part.ThoughtSignature;
            }
        }

        internal static void ButlerToGeminiThoughtFetch(ButlerChatMessageContentPart chatPart, Part part)
        {
            if (chatPart.ProviderSpecific.TryGetValue(GeminiThoughSigKey, out string? Thinking))
            {
                if (Thinking is not null)
                    part.ThoughtSignature = Thinking;
            }
        }
    }
    public static class TranslatorChatCompletionObjects
    {
        public static IButlerChatCompletionOptions TranslateFromProvider(GenerationConfig Options)
        {
            ButlerChatCompletionOptions convertedOptions = new ButlerChatCompletionOptions();

            if (Options.MaxOutputTokens is not null)
            {
                convertedOptions.MaxOutputTokenCount = Options.MaxOutputTokens;
            }

            if (Options.Temperature is not null)
            {
                convertedOptions.Temperature = Options.Temperature;
            }

            if (Options.TopP is not null)
            {
                convertedOptions.TopP = Options.TopP;
            }

            if ((Options.StopSequences is not null) && (Options.StopSequences.Count > 0))
            {
                foreach (var stop in Options.StopSequences)
                {
                    convertedOptions.StopSequences.Add(stop);
                }
#if DEBUG
                Debug.WriteLineIf(convertedOptions.StopSequences.Count > 5, $"Gemini Provider Stop sequences set to {string.Join(",", convertedOptions.StopSequences)}. Note Gemini Doc says 5 is the limit.");
#endif
            }

            {
                // Tools
                
            }


            {
                // frequency penalty
                if (Options.FrequencyPenalty is not null)
                {
                    convertedOptions.FrequencyPenalty = Options.FrequencyPenalty;
                }
            }

            {
                // presence penalty
                if (Options.PresencePenalty is not null)
                {
                    convertedOptions.PresencePenalty = Options.PresencePenalty;
                }
            }

            {
                // seed
                if (convertedOptions.Seed is not null)
                {
                    convertedOptions.Seed = Options.Seed;
                }
            }

            {
                // ResponseFormat
                // my underlying class doesn't support that yet
            }

            {
                // user id
                // apparently Gemini don't do that here
            }

            {
                // parallel tool calls
                // apparently Gemini don't do that here
            }
            return convertedOptions;
        }

        /// <summary>
        /// This is placeholder for different design. Not used.
        /// </summary>
        /// <param name="ButlerOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static GenerationConfig TranslateToProvider(IButlerChatCompletionOptions ButlerOptions)
        {
            throw new NotImplementedException();
        }
    }

    public static class TranslatorRole
    {
        public static ButlerChatMessageRole? TranslateFromProvider(string? role)
        {
            if (role is null)
                return null;
            switch (role)
            {
                case "user":
                    return  ButlerChatMessageRole.User;
                case "model":
                    return ButlerChatMessageRole.Assistant;
                //case "model":             /* important. This is here for completion sake. Yes Tool call is model role in Gemini */
                //  return ButlerChatMessageRole.ToolCall;
                case "function":
                    return ButlerChatMessageRole.ToolResult;
                case "system":
                    throw new NotImplementedException("System is not handled this way by provider. Gemini has no special role for system in the chat log. Provider harvests the system messages and places at the system prompt variable.");
                default:
                    throw new NotImplementedException("Unknown role!");
            }
        }
        public static string TranslateToProvider(ButlerChatMessageRole Role)
        {
            switch (Role)
            {
                case ButlerChatMessageRole.Assistant:
                    return "model";
                case ButlerChatMessageRole.User:
                    return "user";
                case ButlerChatMessageRole.ToolCall:
                    return "model";
                case ButlerChatMessageRole.ToolResult:
                    return "function";
                case ButlerChatMessageRole.System:
                    throw new InvalidOperationException("Gemini system messages exist in a separate area of  the Gemini request. Provider harvests system messages (Role= ButlerChatMessageRole.System) and moves them there. No equivalent system message");
                default:
                    throw new NotImplementedException("Unknown Role to translate to Gemini. May need to check if there's additional roles for the provider");
            }
        }
    }
    public static class TranslatorChatMessage
    {
        public static Content TranslateToProvider(ButlerChatMessage msg)
        {
#if DEBUG
            /*
            Debug.WriteLineIf(msg.Role == ButlerChatMessageRole.None, "Debug Warning: Attempting to translate message with unspecified role to Gemini. Defaulting to treating as user message. Ensure message is classified OK. This warning does not fire in release mode");
            if (msg.Role == ButlerChatMessageRole.None)
                msg.Role = ButlerChatMessageRole.User;*/
#endif
            Content GeminiProviderMsg = new Content();
            switch (msg.Role)
            {
                case ButlerChatMessageRole.Assistant:
                    GeminiProviderMsg.Role = "model";
                    break;
                case ButlerChatMessageRole.User:
                    GeminiProviderMsg.Role = "user";
                    break;
                case ButlerChatMessageRole.System:
                    {
                        throw new InvalidOperationException("This part should not actually be called, system prompts are handled at the chat log level");
                    }
                case ButlerChatMessageRole.ToolCall:
                {
                        GeminiProviderMsg.Role = "model";
                        if (msg  is ButlerChatToolCallMessage ToolCall)
                        {
                            Part GeminiCall = new Part();
                            GeminiCall.FunctionCall = new FunctionCall(ToolCall.ToolName);

                            GeminiCall.FunctionCall.Args = ToolCall.FunctionArguments;
                            for (int thoughts = 0; thoughts < msg.Content.Count; thoughts++)
                            {
                                GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(msg.Content[thoughts], GeminiCall);
                            }
                            GeminiProviderMsg.Parts.Add(GeminiCall);
                            return GeminiProviderMsg;
                        }
                        else
                        {
                            throw new InvalidOperationException("Tool call message isn't actually a ButlerToolCall was misplaced");
                        }
                }
            }
            
            
            // not a function call
            foreach (ButlerChatMessageContentPart p in msg.Content)
            {
                if (p.MessageType == ButlerChatMessageType.Text)
                {
                    Part GeminiPart = new Part();
                    if (p.Refusal is not null)
                    {
                        GeminiPart.Text = p.Refusal;
                    }
                    else
                    {
                        GeminiPart.Text = p.Text;
                    }
                    GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(p, GeminiPart);

                    GeminiProviderMsg.AddPart(GeminiPart);
                }
                else
                {
                    throw new NotImplementedException("Currently Gemini Provider only supports text");
                }
            }
         
            return GeminiProviderMsg;
            
        }   
        public static ButlerChatMessage TranslateFromProvider2(GenerateContentResponse response)
        {
            ArgumentNullException.ThrowIfNull(nameof(response));
            ButlerChatMessage msg = new ButlerChatMessage();
            msg.Id = response.ResponseId;
            msg.Role = ButlerChatMessageRole.Assistant;
            msg.Content = new();
            msg.Content.Add(new ButlerChatMessageContentPart()
            {
                MessageType = ButlerChatMessageType.Text,
                Text = response.Text
            });

            msg.Participant = response.ModelVersion;
            if (response.PromptFeedback is not null)
            {
                if (response.PromptFeedback.SafetyRatings is not null)
                {
                    msg.Content.Add(new ButlerChatMessageContentPart()
                    {
                        MessageType = ButlerChatMessageType.Refusal,
                        Refusal = TranslatorSafetyRating.TranslateFromProvider(response.PromptFeedback)
                    });
                }
            }
            return msg;
        }
    }


    /// <summary>
    /// This Translator Takes the tools specified in <see cref="IButlerChatCompletionOptions"/>, uses the <see cref="IButlerLLMProvider"/> passed (SHOULD BE GEMINI One) and dumps the objects for the tools contained in the <see cref="GenerateContentRequest.Tools"/> area as a single tool of multiple functions
    /// </summary>
    public static class TranslatorChatTools
    {
        /// <summary>
        /// TLDR description: For the tools described by in options, use the Gemini provider chat create tool and place them in the context built as single tool section of multiple functions
        /// </summary>
        /// <param name="ContextBuilt">where to place butler tools. Note places as single <see cref="GenerativeAI.Types.Tool"/> of multiple <see cref="GenerativeAI.Types.FunctionDeclaration"/></param>
        /// <param name="Options">looks here for <see cref="IButlerChatCompletionOptions.Tools"/></param>
        /// <param name="GeminiProvider">The LLM Provider <see cref="ButlerGeminiProvider"/> we use to create the tool object</param>
        public static void TranslateTools(GenerateContentRequest ContextBuilt, IButlerChatCompletionOptions Options, IButlerLLMProvider GeminiProvider )
        {
            /*
             * Dear Reader:
             * Gemini's provider treats the tools Butler presents as a collection of functions housed a single Gemini Tool object class
             */


            Tool GeminiToolCollection = new();
            GeminiToolCollection.FunctionDeclarations = new List<FunctionDeclaration>();
            foreach (var ButlerTool in Options.Tools)
            {
                
                // in referent to dear reader: Butler's Gemini provider dumps the tools (from its pov) into a single tool with several functions.
                 var GeminiFunction = (FunctionDeclaration)GeminiProvider.CreateChatTool(ButlerTool);
                GeminiToolCollection.FunctionDeclarations.Add(GeminiFunction);
            }
            ContextBuilt.AddTool(GeminiToolCollection);
        }
    }
    public static class TranslatorChatLog
    {
        static readonly JsonNode? NoArgsGeminiParse = JsonNode.Parse("{}");
        const string ToolReplyCastingErrorMessageState  = "Data conversion (cast) error. ButlerMessage is flagged as a Tool reply via the role BUT can't cast it to a ButlerChatToolResultMessage type";
        const string ToolCallCastingErrorMessageState = "Data conversion (cast) error. ButlerMessage is flagged as a Tool Call via the role BUT can't cast it to a ButlerChatToolCallMessage type";
        const string IDNullErrorMessageToolCall = "Please Ensure the tool call ID is not null nor empty. Note the normal tool resolver will actually ensure non null on scheduling.";
        const string IDNullErrorMessageToolResult = "Please Ensure the tool results ID is not null nor empty. Note the normal tool resolver will actually ensure non null on scheduling.";
        /// <summary>
        /// For tool calls, this * the call id serve to hold the place of a tool response before the translator plugs it in.
        /// </summary>
        const string MarkerReplyIndex = "PlaceHolderReply";
        /// <summary>
        /// For tool calls, this * the call id serve to hold the place of a tool call before the translator plugs it in.
        /// </summary>
        const string MarkerCallIndex = "PlaceHolderCall";
 public static GenerateContentRequest TranslateToProvider(IList<ButlerChatMessage> Messages)
        {
            
            ArgumentNullException.ThrowIfNull(Messages);
            
            static void UpdateCallMe(
                Dictionary<string,
                                  (ButlerChatToolCallMessage CallMe,
                                  ButlerChatToolResultMessage CallMeResult)> DB,
                ButlerChatToolCallMessage? NewToolCall,
                ButlerChatToolResultMessage? NewToolResult

                )
            {

                /* this routine extracts the ID from either NewToolCall or NewToolResult, using that as key
                 * it will FAIL if presented not defined ID. 
                 * note: code in the chat converter *doe* add ID (thru the resolver if not set)
                 * and will populate field automatically */
                string? ID = null;
                if (NewToolCall is not null)
                {
                    ID = NewToolCall.Id;
                    if (ID is null)
                    {
                        throw new InvalidOperationException(IDNullErrorMessageToolCall);
                    }
                }
                else
                {
                    if (NewToolResult is not null)
                    {
                        ID = NewToolResult.Id;
                        if (ID is null)
                        {
                            throw new InvalidOperationException(IDNullErrorMessageToolCall);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                /* generate idea with above code is if the tool all or reply is NOT null but ID is, complain. Then assign ID string*/
                
                

                (ButlerChatToolCallMessage CallMe, ButlerChatToolResultMessage CallMeResult) Entry;
                if (DB.TryGetValue(ID, out Entry))
                {
                    if (NewToolCall is not null)
                    {
                        Entry.CallMe = NewToolCall;
                    }
                    if (NewToolResult is not null)
                    {
                        Entry.CallMeResult = NewToolResult;
                    }
                }
                else
                {
                    Entry = new();
                    if (NewToolCall is not null)
                    {
                        Entry.CallMe = NewToolCall;
                    }
                    if (NewToolResult is not null)
                    {
                        Entry.CallMeResult = NewToolResult;
                    }
                }
                DB[ID] = Entry;
            }
            // TickOrder is only relevant if we got tool calls
            Queue<string> TicketOrder = new();

            /* system prompts are collected here and the request variable is what's returned*/
            List<ButlerChatMessage> sys = new List<ButlerChatMessage>();
            GenerateContentRequest request = new GenerateContentRequest();

            /* OK future you, the plan is call id = key
             * 
             * Found a matching tool call - assign here to call id.
             * Found a matching tool result - same
             * check for no nulls - sanity check
             * 
             * then go create the parts that have everything in Gemini *; &*/
            Dictionary<string, (ButlerChatToolCallMessage CallMe, ButlerChatToolResultMessage CallMeResult)> toolCalls = new();

            
            for (int i = 0; i < Messages.Count; i++)
            {
                ButlerChatMessage msg = Messages[i];
                // collect system prompt from the list to past into request system prompt later.
                // or call the translator chat message thing
                if (msg.Role == ButlerChatMessageRole.System)
                {
                    sys.Add(msg);
                    continue;
                }
                if (msg.Role == ButlerChatMessageRole.ToolCall)
                {
                    // create the place holder for a tool call, while we can safely 
                    // assume the reply is be probably following, we actually need to be paranoid about fixing it up.
                    // hence the stub and fix in post
                    ButlerChatToolCallMessage? ToolWanted = msg as ButlerChatToolCallMessage;
                    if (ToolWanted is not null)
                    {
                        UpdateCallMe(toolCalls, ToolWanted, null);
                        if (TicketOrder.Contains(ToolWanted.Id!) == false) /// ! justified because code above should pump if ToolWanted not null and ID IS
                        {
                            TicketOrder.Enqueue(ToolWanted.Id!); /// ! justified because code above should pump if ToolWanted not null and ID IS
                        }
                        var NewEntry = new Content(MarkerCallIndex, ToolWanted.Id);

                        request.AddContent(NewEntry);

                        // ensure thought is there.
                        // we do assume the code calling is is not trying multiple thoughts in one shot.
                        if (
                            ((ToolWanted.Content is not null) && (request.Contents is not null)) && ((ToolWanted.Content.Count != 0) && (request.Contents.Count != 0))
                           )
                            GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(ToolWanted.Content[0], request.Contents[0].Parts[0]);
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException(ToolCallCastingErrorMessageState);
                    }
                }
                if (msg.Role == ButlerChatMessageRole.ToolResult)
                {
                    // create the place holder for a tool call, while we can safely 
                    // assume the reply is be probably following, we actually need to be paranoid about fixing it up.
                    // hence the stub and fix in post
                    ButlerChatToolResultMessage? ToolResult = msg as ButlerChatToolResultMessage;
                    if (ToolResult is not null)
                    {
                        UpdateCallMe(toolCalls, null, ToolResult);
                        if (TicketOrder.Contains(ToolResult.Id!) == false) /// ! justified because code above should dump if ToolResult not null and ID IS
                        {
                            TicketOrder.Enqueue(ToolResult.Id!); /// ! justified because code above should dump if ToolResult not null and ID IS
                        }
                        var NewEntry = new Content(MarkerReplyIndex, ToolResult.Id);
                        request.AddContent(NewEntry);

                        // ensure thought is there.
                        // we do assume the code calling is is not trying multiple thoughts in one shot.
                        if (
                          ((ToolResult.Content is not null) && (request.Contents is not null)) && ((ToolResult.Content.Count != 0) && (request.Contents.Count != 0))
                         )
                            GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(ToolResult.Content[0], request.Contents[0].Parts[0]);

                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException(ToolReplyCastingErrorMessageState);
                    }
                }


                // normal processing. Add to the parts list
                request.AddContent(TranslatorChatMessage.TranslateToProvider(msg));

            }
            // Build the system prompt. Note we only care about a prompt for system in the provider IF it is text

            request.SystemInstruction = new Content();
            foreach (var SystemPromptMessage in sys)
            {
                if (SystemPromptMessage.Content.Count != 0)
                {
                    foreach (var data in SystemPromptMessage.Content)
                    {
                        if (data.MessageType == ButlerChatMessageType.Text)
                        {
                            if (data.Text is not null)
                                request.SystemInstruction.AddText(data.Text);
                        }
                    }
                }
            }


            // bit of a sanity ensure null doesn't trigger detonation
            if (request.Contents is null)
            {
                request.Contents = new List<Content>();
            }

            if (request.Contents.Count == 0)
            {
                // hard coded user prompt to prompt Gemini to get going
                request.AddPart(new Part("Hello"));
            }

            if (TicketOrder.Count != 0)
            {
                while (TicketOrder.Count != 0)
                {
                    Content ToolCallRequest = new();
                    Content ToolCallReply = new();
                    var NextTicker = TicketOrder.Dequeue();
                    Part ToolRequestPart = new();
                    Part ToolReplyPart = new();


                    var OrderUp = toolCalls[NextTicker];
                    // plug in the data for the replacement for the place holder function call
                    ToolRequestPart.FunctionCall = new();
                    ToolRequestPart.FunctionCall.Name = OrderUp.CallMe.ToolName;
                    ToolRequestPart.FunctionCall.Id = NextTicker; // which is  the id
                    
                    try
                    {
                        if (OrderUp.CallMe.FunctionArguments is not null)
                        {
                            // possible questionable temporary fix
                            var DictionaryTmp = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonNode.Parse(OrderUp.CallMe.FunctionArguments));


                            ToolRequestPart.FunctionCall.Args = JsonSerializer.SerializeToNode(DictionaryTmp);
                        }
                        else
                        {
                            ToolRequestPart.FunctionCall.Args = TranslatorChatLog.NoArgsGeminiParse;
                        }
                        
                        // original code
                        //ToolRequestPart.FunctionCall.Args = JsonNode.Parse(OrderUp.CallMe.FunctionArguments);
                    }
                    catch (JsonException)
                    {
                        // guard against a tool having null as arguments. we swap a blank arg
                        ToolRequestPart.FunctionCall.Args = TranslatorChatLog.NoArgsGeminiParse;
                    }

                    // don't forget to add
                    ToolCallRequest.AddPart(ToolRequestPart);

                    
                    // plug in the replacement for the place holder reply
                    ToolReplyPart.FunctionResponse = new();
                    ToolReplyPart.FunctionResponse.Name = OrderUp.CallMe.ToolName;
                    ToolReplyPart.FunctionResponse.Id = NextTicker;
                    ToolReplyPart.FunctionResponse.Response = OrderUp.CallMeResult.Message;
                    ToolCallReply.AddPart(ToolReplyPart);

                    /* while a tool can and likely should return json node level strictness
                     *  we can't assume it. Failure to parse punts it to a general json results message aka [result] = "original tool reply"
                     *  */
                        try
                        {
                            JsonNode? JsonParseAttempt=null;
                            if (OrderUp.CallMeResult.Message is not null)
                            {
                              JsonParseAttempt   = JsonNode.Parse(OrderUp.CallMeResult.Message);
                            }

                            if (JsonParseAttempt is null)
                            {
                                ToolReplyPart.FunctionResponse.Response = new JsonObject
                                {
                                    ["result"] = OrderUp.CallMeResult.Message
                                };
                            }
                            else
                            {
                                ToolReplyPart.FunctionResponse.Response = JsonParseAttempt;
                            }
                           
                        }
                        catch (JsonException)
                        {
                            ToolReplyPart.FunctionResponse.Response = new JsonObject
                            {
                                ["result"] = OrderUp.CallMeResult.Message
                            };
                        }

                    
                    ToolCallRequest.Role = "model"; // the function call is a request *from the model*
                    ToolCallReply.Role = "function"; // the function reply is input *outside the model*


                    for (int i = 0; i < request.Contents.Count; i++)
                    {
                        var checkThis = request.Contents[i];
                        if (checkThis.Parts.Count == 1)
                        {
 
                            // we set the function call and reply earlier in the routine, the ! is justified i think
                            if ( (checkThis.Role == ToolCallRequest.Parts[0].FunctionCall!.Id) && (checkThis.Parts[0].Text == MarkerCallIndex))
                            {
                                {
                                    request.Contents[i] = null!; // remove the place holder
                                    request.Contents[i] = ToolCallRequest; // put the real one
                                }
                                
                            }
                            else
                            {
                                if ( (checkThis.Role == ToolCallReply.Parts[0].FunctionResponse!.Id) && (checkThis.Parts[0].Text == MarkerReplyIndex))
                                    {
                                    
                                    {
                                        request.Contents[i] = null!; // remove the place holder
                                        request.Contents[i] = ToolCallReply; // put real one
                                    }
                                }

                            }
                        }
                    }
                 

 
                }
            }

            DebugSettings.VerboseLogHandler(Messages, request);
            return request;
            
        }
       
    }

    /// <summary>
    /// Dummy interface to represent a generative model from the Gemini SDK for Unit Tests and mocking, Internal creation of this object is literally just
    /// </summary>
    public interface IGenerativeModel
    {
        public Task<GenerateContentResponse> GenerateContentAsync(GenerateContentRequest request);
        public IAsyncEnumerable<GenerateContentResponse> StreamContentAsync(GenerateContentRequest request);
    }

    /// <summary>
    /// Small stuff/shim class to enable working <see cref="ButlerGeminiChatClient"/>  and mock-able one.
    /// </summary>
    class GenericModelForward : IGenerativeModel
    {
        public GenericModelForward(GenerativeModel Target)
        {
            this.Target = Target;
        }
        GenerativeModel Target;
        public Task<GenerateContentResponse> GenerateContentAsync(GenerateContentRequest request)
        {
            return Target.GenerateContentAsync(request);
        }

        public IAsyncEnumerable<GenerateContentResponse> StreamContentAsync(GenerateContentRequest request)
        {
            return Target.StreamContentAsync(request);
        }
    }

    public class ButlerGeminiChatClient: IButlerChatClient
    {
        IGenerativeModel Client;
        IButlerLLMProvider Source;
        IButlerChatPreprocessor? PPR;
        //ChatSession Session;
        
        public ButlerGeminiChatClient(IGenerativeModel Client, IButlerLLMProvider Source, IButlerChatPreprocessor? PPR)
        {
            ArgumentNullException.ThrowIfNull(Client);
            ArgumentNullException.ThrowIfNull(Source);
            ArgumentNullException.ThrowIfNull(Client);

            this.Client = Client;
            this.Source = Source;
            this.PPR = PPR;
          
        }

        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            IList<ButlerChatMessage> PPRMSG;
            if (PPR is not null)
            {
                PPRMSG = PPR.PreprocessMessages(msg).ToList();

            }
            else
            {
                PPRMSG = msg.ToList();
            }
            // do messages
            var chat = TranslatorChatLog.TranslateToProvider(PPRMSG);
            // do tool
            var response = Client.GenerateContentAsync(chat);
            return new ButlerGeminiClientResult(response);
        }

        public IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            // step 1: trigger PPR id defined, use that instead of msg arg
            IList<ButlerChatMessage> PPRMSG;
            if (PPR is not null)
            {
                PPRMSG = PPR.PreprocessMessages(msg).ToList();

            }
            else
            {
                PPRMSG = msg.ToList();
            }

            // Gemini is a call Translator. code 
            var chat = TranslatorChatLog.TranslateToProvider(PPRMSG);
            // this function adds the blasted tools to Gemini's data source so it knows to call them
           TranslatorChatTools.TranslateTools(chat, options, this.Source);
      

            // send this to Gemini and return the wrapper to process it
            var response = Client.StreamContentAsync(chat);
            return new ButlerGeminiCollectionResult<ButlerStreamingChatCompletionUpdate>(response);
        }

        public async IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, [EnumeratorCancellation] CancellationToken cancelMe = default)
        {


            List<ButlerChatMessage> PPRMSG;
            //IList<ButlerChatMessage> PPRMSG;
            PPRMSG = new List<ButlerChatMessage>();
                if (PPR is not null)
                {
                    var tmp = PPR.PreprocessMessages(msg);
                foreach (var msg_walk in tmp)
                    PPRMSG.Add(msg_walk);

                }
                else
                {
                    foreach (var msg_walk in msg)
                    {
                        PPRMSG.Add(msg_walk);
                    }
                }
                


            // Gemini is a call translator. code 
            var chat = TranslatorChatLog.TranslateToProvider(PPRMSG);
                // this function adds the blasted tools to Gemini's data source so it knows to call them
                TranslatorChatTools.TranslateTools(chat, options, this.Source);
    

                // send this to Gemini and return the wrapper to process it
                var response = Client.StreamContentAsync(chat);

                await foreach (var reply in response.WithCancellation(cancelMe))
                {
                    if (reply is not null)
                    {
                        var butlerPart = TranslatorStreamingChatUpdate.TranslateFromProvider(reply);
                        yield return butlerPart;
                    }
                    continue;
                }
            

        
        }

        public IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            return CompleteChatStreamingAsync(msg, options, default);
        }
    }

    public static class TranslatorSafetyRating
    {
        public static string? TranslateFromProvider(PromptFeedback feedback)
        {
            if (feedback is null)
                return null;
            else
            {
                string ret = string.Empty;
                if (feedback.SafetyRatings is not null)
                {
                    foreach (SafetyRating x in feedback.SafetyRatings)
                    {
                        ret += $"Prompt Classification: {x.Category}:{x.Blocked};";
                    }
                }
                if (feedback.BlockReasonMessage is not null)
                {
                    ret += $"Your prompt was blocked for the following reason(s): {feedback.BlockReasonMessage}\r\n";
                }
                return ret;
            }
        }   
    }

    public static class TranslatorFinishReason
    {
        public static ButlerChatFinishReason? TranslateFromProvider(FinishReason? FINISH)
        {
            if (FINISH is null)
            {
                return null;
            }
            else
            {
               switch (FINISH)
                {
                    case FinishReason.BLOCKLIST:
                    case FinishReason.RECITATION:
                    case FinishReason.SAFETY:
                    case FinishReason.IMAGE_SAFETY:
                        return ButlerChatFinishReason.ContentFilter;
                    case FinishReason.STOP:
                        return ButlerChatFinishReason.Stop;
                    case FinishReason.MAX_TOKENS:
                        return ButlerChatFinishReason.Length;
                    case FinishReason.SPII:
                        return ButlerChatFinishReason.ContentFilter;
                    default:
                        throw new NotImplementedException();
                }
            }
                throw new NotImplementedException();
        }

     }
        

    
    public static class TranslatorStreamingChatUpdate
    {
        static void TranslateCandidate(ButlerStreamingChatCompletionUpdate update, GenerativeAI.Types.Candidate reply)
        {
#if DEBUG
            // for Gemini pain, we collect the message parts and this lets the debugger (user) inspect as it goes
            List<ButlerChatStreamingPart> DebugParts = new();
#endif
            if (reply.FinishReason is not null)
            {
                update.FinishReason = TranslatorFinishReason.TranslateFromProvider(reply.FinishReason);
            }
            
            List<FunctionCall> functionCallCollection = new List<FunctionCall>();
            if (reply.Content is not null)
            {
                for (int i = 0; i < reply.Content.Parts.Count; i++)
                {
                    var part = reply.Content.Parts[i];
                    ButlerChatStreamingPart chatPart = new ButlerChatStreamingPart();
#if DEBUG
                    DebugParts.Add(chatPart);
#endif
                    chatPart.Text = part.Text;
                    chatPart.Kind = ButlerChatMessagePartKind.Text;


                    if (part.ThoughtSignature is not null)
                    {
                        GeminiAssist_ThoughtSigHelper.GeminiToButlerThoughtStore(part, chatPart);
                    }
                    if (part.FunctionCall is not null)
                    {
                        functionCallCollection.Add(part.FunctionCall);
                        //continue;
                    }

                    if ((!((string.IsNullOrEmpty(chatPart.Text)))))
                    {
                        update.EditorableContentUpdate.Add(chatPart);
                    }
                    else
                    {
                     
                    }

                }
            }

            if (functionCallCollection.Count is not 0)
            {
                for (int step=0; step < functionCallCollection.Count; step++)
                {
                   
                    
                    update.FunctionName = functionCallCollection[step].Name;
                    if ((functionCallCollection[step] is not null) && (functionCallCollection[step].Args is not null))
                    {
                        update.FunctionArgumentsUpdate = functionCallCollection[step].Args!.ToString(); // ! seemingly justified cause the if statement this is in
                    }
                    else
                    {
                        update.FunctionArgumentsUpdate = null;
                    }
                        update.Id = functionCallCollection[step].Id;
                    var ButlerToolPart = new ButlerStreamingToolCallUpdatePart(update.FunctionName, update.FunctionArgumentsUpdate, update.Index, "Function", functionCallCollection[step].Id!);
                    ButlerToolPart.FunctionArgumentsUpdate = update.FunctionArgumentsUpdate;
                    ButlerToolPart.FunctionName = update.FunctionName;
                    ButlerToolPart.Index = update.Index;
                    ButlerToolPart.ToolCallid = functionCallCollection[step].Id!; //  questionable at best. My thinking is that the pathway to this translator is the tool scheduler ensures its not null when converting and the messaging system in theory should fix it later

                    update.EditableToolCallUpdates.Add(ButlerToolPart);


                    /*var ButlerToolPart = new ButlerStreamingToolCallUpdatePart();
                    
                    update.FunctionName = functionCallCollection[step].Name;
                    if ((functionCallCollection[step] is not null) && (functionCallCollection[step].Args is not null))
                    {
                        update.FunctionArgumentsUpdate = functionCallCollection[step].Args!.ToString(); // ! seemingly justified cause the if statement this is in
                    }
                    else
                    {
                        update.FunctionArgumentsUpdate = null;
                    }
                        update.Id = functionCallCollection[step].Id;

                    ButlerToolPart.FunctionArgumentsUpdate = update.FunctionArgumentsUpdate;
                    ButlerToolPart.FunctionName = update.FunctionName;
                    ButlerToolPart.Index = update.Index;
                    ButlerToolPart.ToolCallid = functionCallCollection[step].Id!; //  questionable at best. My thinking is that the pathway to this translator is the tool scheduler ensures its not null when converting and the messaging system in theory should fix it later

                    update.EditableToolCallUpdates.Add(ButlerToolPart);*/

                }
                update.FinishReason = ButlerChatFinishReason.ToolCalls;
            }
            return; // for breakpoint
        }

        public static ButlerStreamingChatCompletionUpdate TranslateFromProvider(GenerateContentResponse response)
        {
            ButlerStreamingChatCompletionUpdate update = new ButlerStreamingChatCompletionUpdate();

            if (response.Candidates is not null)
            {
                foreach (var result in response.Candidates)
                {
                    TranslateCandidate(update, result);
                }
            }

            
            update.CompletionId = response.ResponseId;
            
      
            return update;
        }
    }

    public class ButlerGeminiCollectionResult<T> : IButlerCollectionResult<ButlerStreamingChatCompletionUpdate>
    {
        IAsyncEnumerable<GenerateContentResponse> Response;
        public ButlerGeminiCollectionResult(IAsyncEnumerable<GenerateContentResponse> response)
        {
            ArgumentNullException.ThrowIfNull(response);
            Response = response;
        }



        public IEnumerator<ButlerStreamingChatCompletionUpdate> GetEnumerator()
        {
            // The "Safe" Sync-over-Async Bridge.
            // 1. Get the async enumerator
            var walk = Response.ToBlockingEnumerable();

            
                    foreach (var item in walk)
                    {
                        yield return TranslatorStreamingChatUpdate.TranslateFromProvider(item);
                    }

        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ButlerGeminiClientResult : IButlerClientResult
    {
        Task<GenerateContentResponse> Response;

        /// <summary>
        /// Anything tasks for the task to be done, does it and sets this to be true
        /// </summary>
        bool Resolved = false;
        public ButlerGeminiClientResult(Task<GenerateContentResponse> response)
        {
            ArgumentNullException.ThrowIfNull(response);
            Response = response;
        }

        void ResolveCheck()
        {
            if (!Resolved)
            {
                Response.Wait();
                Resolved = true;
            }
        }
        public byte[]? GetBytes()
        {
            ResolveCheck();
            var result = GetResult();
            if (result is not null) return System.Text.Encoding.UTF8.GetBytes(result);
            return null;
        }

        public string? GetResult()
        {
            ResolveCheck();
            return Response.Result.Text;
        }

        public ButlerClientResultType GetResultType()
        {
            ResolveCheck();
            return ButlerClientResultType.String;
        }
    }
}
