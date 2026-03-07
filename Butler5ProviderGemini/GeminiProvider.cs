using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Providers.Gemini;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using GenerativeAI.Types;
using SecureStringHelper;
using System.Collections;
#if DEBUG
using System.Diagnostics;

#endif
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ButlerSDK.Providers.Gemini
{

    internal static class DebugMode
    {
        static readonly JsonSerializerOptions Mode = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true // Makes the JSON pretty-printed in the console
        };
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
        static FlagMode Mode = FlagMode.Off;

        static Stream? SetTranslatorVerboseOutputMode_Butler = null;
        static Stream? SetTranslatorVerboseOutputMode_Gemini = null;

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
    public class ButlerGeminiProvider : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProviderToolRequests, IButlerLLMProvider_SpecificToolExecutionPostCall
    {
        GenerativeAI.GoogleAi? api;
        /* has unit tests*/
        public ButlerGeminiProvider()
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

        static bool SetOK = false;
        static JsonSerializerOptions Preferred = new();
        
        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            // HELPER FUNCTION: Add this inside your ButlerGeminiProvider class
               static void FixGeminiTypes(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                // If this object has a "type" property, force it to uppercase
                if (obj.TryGetPropertyValue("type", out var typeNode) &&
                    typeNode is JsonValue val &&
                    val.TryGetValue<string>(out var typeStr))
                {
                    obj["type"] = typeStr.ToUpperInvariant();
                }

                // Recursively check all nested properties (e.g., inside "properties" or "items")
                foreach (var kvp in obj)
                {
                    FixGeminiTypes(kvp.Value);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    FixGeminiTypes(item);
                }
            }
        }

            if (!SetOK)
            {
                Preferred.PropertyNameCaseInsensitive = true;
                Preferred.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                SetOK = true;
            }
            FunctionDeclaration func = new FunctionDeclaration();
            func.Name = butlerToolBase.ToolName;
            //func.Behavior = Behavior.BLOCKING; // Gemini Provider supports blocking only for now as a description.
            func.Description = butlerToolBase.ToolDescription;

            try
            {
                var args = JsonNode.Parse(butlerToolBase.GetToolJsonString());
                FixGeminiTypes(args);

                func.Parameters = JsonSerializer.Deserialize<Schema>(args, Preferred);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"Tool {butlerToolBase.ToolName} has invalid JSON schema. Ensure the JSON schema is valid and properly escaped if needed. Original json: {butlerToolBase.GetToolJsonString()}");
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

            IGenerativeModel? gen_model = new GenericModelForward(provider_client);
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

        public void HandlerToolExecuteRequestMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolCallMessage Item)
        {
            if (ProviderSpecific.TryGetValue(GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey, out string? Thinking))
            {
                if (Thinking is not null)
                {
                    Item.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey] = Thinking;
                }
            }
        }

        public void HandlerToolExecuteMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolResultMessage Item)
        {

                if (ProviderSpecific.TryGetValue(GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey, out string? Thinking))
                {
                    if (Thinking is not null)
                    {
                        Item.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey] = Thinking;
                    }
                }
            }
        }
    }

namespace ButlerSDK.Providers.Gemini
{
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
        public const string GeminiThoughSigKey = "GeminiModelThinking";

        internal static void GeminiToButlerThoughtStore(Part part, ButlerChatStreamingPart chatPart)
        {
            if (part.ThoughtSignature is not null)
            {
                chatPart.ProviderSpecfic[GeminiThoughSigKey] = part.ThoughtSignature;
            }
        }

        internal static bool GeminiPartHasThought(Part Part)
        {
            if (Part.Thought == true)
            {
                if (Part.ThoughtSignature is not null)
                {
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Part.ThoughtSignature is not null)
                {
                    return true;
                }
            }
            return false;
        }
        internal static void ButlerToGeminiThoughtFetch(ButlerChatMessageContentPart chatPart, Part part)
        {
            if (chatPart.ProviderSpecific.TryGetValue(GeminiThoughSigKey, out string? Thinking))
            {
                if (Thinking is not null)
                    part.ThoughtSignature = Thinking;
            }
        }

        internal static string? ReadSig(ButlerChatToolCallMessage callMe)
        {
            for (int i = 0; i < callMe.Content.Count; i++)
            {
                if (callMe.Content[i].ProviderSpecific.TryGetValue(GeminiThoughSigKey, out string? Thinking))
                {
                    return Thinking;
                }
            }

            if (callMe.ProviderSpecific.TryGetValue(GeminiThoughSigKey, out string? ThinkingAlt))
            {
                return ThinkingAlt;
            }
            return null;
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
                    return ButlerChatMessageRole.User;
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
                    return "user";// the question is why did function work in old code?
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
                        if (msg is ButlerChatToolCallMessage ToolCall)
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
        public static void TranslateTools(GenerateContentRequest ContextBuilt, IButlerChatCompletionOptions Options, IButlerLLMProvider GeminiProvider)
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
        enum tool_seeker
        {
            offline = 0,
            foundcall = 1,
            foundreply = 2
        }

        static bool RoleValidCheck(ButlerChatMessageRole Current, ButlerChatMessageRole Last)
        {
            // the only postiive true always is if last is 0;
            if (Last == ((ButlerChatMessageRole)(-1)))
                return true;
            else
            {
                if (Current == Last)
                    return false;
                else
                    return true;
            }
        }

        static void PlaceToolCall(GenerateContentRequest Target, ButlerChatToolCallMessage CallMe, ButlerChatToolResultMessage ReplyMe)
        {
            Content ToolCall = new();
            Content ToolReply = new();
            ToolCall.Role = TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolCall);
            ToolReply.Role = TranslatorRole.TranslateToProvider(ButlerChatMessageRole.ToolResult);
            //for (int i =0; i < CallMe.Content.Count;i++)
            {

                Part CPart = new Part();
                CPart.ThoughtSignature = GeminiAssist_ThoughtSigHelper.ReadSig(CallMe);
                CPart.FunctionCall = new();
                CPart.FunctionCall.Name = CallMe.ToolName;
                CPart.FunctionCall.Id = CallMe.Id;
                if (!string.IsNullOrEmpty(CallMe.FunctionArguments))
                {
                    CPart.FunctionCall.Args = JsonNode.Parse(CallMe.FunctionArguments);
                }
                else
                {
                    CPart.FunctionCall.Args = null;
                }

                ToolCall.Parts.Add(CPart);
            }


            //for (int i = 0; i < ReplyMe.Content.Count; i++)
            {
                Part CPart = new Part();
                //   CPart.ThoughtSignature = GeminiAssist_ThoughtSigHelper.ReadSig(CallMe);
                CPart.FunctionResponse = new();
                CPart.FunctionResponse.Name = CallMe.ToolName;
                CPart.FunctionResponse.Id = CallMe.Id;
                Dictionary<string, object> Results = new();
                Results["Result"] = ReplyMe.GetCombinedText();
                try
                {
                    var Doc = JsonSerializer.Serialize(Results);
                    if (Doc is not null)
                    {
                        var Node = JsonNode.Parse(Doc);
                        if (Node is not null)
                        {
                            CPart.FunctionResponse.Response = Node.Root;
                        }
                    }
                    //CPart.FunctionResponse.Response = JsonNode.Parse(JsonSerializer.Serialize(Results)).Root;
                    if (CPart.FunctionResponse.Response is not null)
                    {
                        throw new InvalidOperationException("Successful json parse BUT did not assign ok to response");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse tool result into Gemini expected format. Ensure the tool result is properly formed and that the Gemini provider can handle the output. Original error: {ex.Message}");
                }

                ToolReply.Parts.Add(CPart);
            }

            Target.Contents.Add(ToolCall);
            Target.Contents.Add(ToolReply);
        }
        public static GenerateContentRequest TranslateToProvider(IList<ButlerChatMessage> Messages, IButlerChatCompletionOptions Options, IButlerLLMProvider GeminiProvider)
        {
            uint callcount = 0;
            uint replycount = 0;
            Queue<ButlerChatMessage> ToolBuffer = new();
            List<ButlerChatMessage> SystemMessage = new();
            GenerateContentRequest request = new();
            string? tool_id = null;
            ButlerChatMessageRole last_role = ((ButlerChatMessageRole)(-1));
            for (int i = 0; i < Messages.Count; i++)
            {

                switch (Messages[i].Role)
                {
                    case ButlerChatMessageRole.System:
                        {
                            SystemMessage.Add(Messages[i]);
                            break;
                        }
                    case ButlerChatMessageRole.ToolCall:
                        {
                            callcount++;
                            if (Messages[i] is ButlerChatToolCallMessage CallMe)
                            {
                                tool_id = CallMe.Id;
                            }
                            else
                            {
                                throw new InvalidDataException("A butler message is tagged as a tool call BUT cannot be cast to a tool call data type. Ensure the message is properly formed and classified.");
                            }
                            ToolBuffer.Enqueue(Messages[i]);
                            break;
                        }
                    case ButlerChatMessageRole.ToolResult:
                        {
                            replycount++;
                            if (Messages[i] is ButlerChatToolResultMessage ReplyMe)
                            {
                                if (string.Compare(ReplyMe.Id, tool_id) == 0)
                                {
                                    ToolBuffer.Enqueue(Messages[i]);
                                    PlaceToolCall(request, (ButlerChatToolCallMessage)ToolBuffer.Dequeue(), (ButlerChatToolResultMessage)ToolBuffer.Dequeue());
                                }
                                else
                                {
                                    throw new InvalidDataException("Unspected tool call id in message log. Gemini translater rqeuires tool calls and results to be in order and have matching IDs. Ensure the message log is properly ordered and formed.");
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("A butler message is tagged as a tool reply BUT cannot be cast to a tool call data type. Ensure the message is properly formed and classified.");
                            }

                            break;
                        }
                    case ButlerChatMessageRole.User:
                        {
                            Content UserEntry = new();
                            UserEntry.Role = TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User);
                            foreach (var part in Messages[i].Content)
                            {
                                if (part.MessageType == ButlerChatMessageType.Text)
                                {
                                    Part GeminiPart = new Part();
                                    if (part.Refusal is not null)
                                    {
                                        GeminiPart.Text = part.Refusal;
                                    }
                                    else
                                    {
                                        GeminiPart.Text = part.Text;
                                    }
                                    GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(part, GeminiPart);
                                    if (GeminiAssist_ThoughtSigHelper.GeminiPartHasThought(GeminiPart))
                                    {
                                        throw new InvalidDataException("Gemini User role does not accept model thoughts.");
                                    }

                                    UserEntry.AddPart(GeminiPart);
                                }
                                else
                                {
                                    throw new NotImplementedException("Currently Gemini Provider only supports text");
                                }
                            }

                            request.Contents.Add(UserEntry);
                            break;
                        }
                    case ButlerChatMessageRole.Assistant:
                        {
                            Content LLMResponse = new();
                            LLMResponse.Role = TranslatorRole.TranslateToProvider(ButlerChatMessageRole.Assistant);
                            foreach (var part in Messages[i].Content)
                            {
                                if (part.MessageType == ButlerChatMessageType.Text)
                                {
                                    Part GeminiPart = new Part();
                                    if (part.Refusal is not null)
                                    {
                                        GeminiPart.Text = part.Refusal;
                                    }
                                    else
                                    {
                                        GeminiPart.Text = part.Text;
                                    }
                                    GeminiAssist_ThoughtSigHelper.ButlerToGeminiThoughtFetch(part, GeminiPart);
                                    LLMResponse.AddPart(GeminiPart);
                                }
                                else
                                {
                                    throw new NotImplementedException("Currently Gemini Provider only supports text");
                                }
                            }
                            request.Contents.Add(LLMResponse);
                            break;
                        }
                }
                last_role = Messages[i].Role;

            }


            string stext = string.Empty;
            for (int i = 0; i < SystemMessage.Count; i++)
            {
                if (SystemMessage[i].Role == ButlerChatMessageRole.System)
                {
                    foreach (var part in SystemMessage[i].Content)
                    {
                        if (part.MessageType == ButlerChatMessageType.Text)
                        {
                            stext += part.Text + "\n";
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(stext) == false)
            {
                request.SystemInstruction = new Content();
                request.SystemInstruction.AddText(stext);
            }

            if (request.Contents is null)
            {
                request.Contents = new List<Content>();
            }
            if (request.Contents.Count == 0)
            {
                request.AddText("Hello");
            }
            else
            {
                // always have the 1st user message if the caller didn't state it.s
                if (request.Contents[0].Role != TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User))
                {
                    var Dummy = new Content();
                    Dummy.AddText("Hello");
                    request.Contents.Insert(0, Dummy);
                    Dummy.Role = TranslatorRole.TranslateToProvider(ButlerChatMessageRole.User);
                }
            }
            TranslatorChatTools.TranslateTools(request, Options, GeminiProvider);

            return request;
        }

        public static GenerateContentRequest TranslateToProvider(IList<ButlerChatMessage> pPRMSG)
        {
            return TranslateToProvider(pPRMSG, new ButlerChatCompletionOptions(), new ButlerGeminiProvider());
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

    public class ButlerGeminiChatClient : IButlerChatClient
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
            var chat = TranslatorChatLog.TranslateToProvider(PPRMSG, options, this.Source);
            // this function adds the blasted tools to Gemini's data source so it knows to call them
            //TranslatorChatTools.TranslateTools(chat, options, this.Source);


            // send this to Gemini and return the wrapper to process it
            var response = Client.StreamContentAsync(chat);
#if DEBUG
            Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\r\n");
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("DEBUG ONLY: This is json of what's been end to the gemini end point");
            Console.WriteLine(JsonSerializer.Serialize(chat, new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
            Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\r\n");
#endif

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

            if (reply.Content is not null)
            {
                if (reply.Content.Role is not null)
                {
                    update.Role = TranslatorRole.TranslateFromProvider(reply.Content.Role);
                }
                int sanity_check = 0;
                foreach (Part P in reply.Content.Parts)
                {
                    if (P.FunctionCall is not null)
                    {
                        string? Args = null;
                        if (P.FunctionCall.Args is not null)
                        {
                            Args = P.FunctionCall.Args.ToJsonString();
                        }
                        else
                        {
                            JsonNode? part = JsonNode.Parse("{}");
                            if (part is not null)
                            {
                                Args = part.ToJsonString();
                            }
                        }

                        var ToolHit = new ButlerStreamingToolCallUpdatePart(P.FunctionCall.Name, Args, 0, "function", P.FunctionCall.Id);
                        if (P.ThoughtSignature is not null)
                        {
                            ToolHit.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey] = P.ThoughtSignature;
                        }
                        else
                        {
                            ToolHit.ProviderSpecific[GeminiAssist_ThoughtSigHelper.GeminiThoughSigKey] = null!;// json when sending back should drop this entry
                        }

                        update.EditableToolCallUpdates.Add(ToolHit);
                        sanity_check++;
                    }


                    if (P.Text is not null)
                    {
                        var chatPart = new ButlerChatStreamingPart();
                        chatPart.Text = P.Text;
                        chatPart.Kind = ButlerChatMessagePartKind.Text;
                        if (!string.IsNullOrEmpty(chatPart.Text))
                        {
                            update.EditorableContentUpdate.Add(chatPart);
                        }
                        sanity_check++;
                    }

                    if (sanity_check != 1)
                    {
                        throw new InvalidOperationException("Received a part with multi non empty settings.");
                    }
                }
            }

            return;
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