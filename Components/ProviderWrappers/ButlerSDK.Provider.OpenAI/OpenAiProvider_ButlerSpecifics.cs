using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using OpenAI.Responses;
using SecureStringHelper;
using System.ClientModel;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using static ButlerSDK.Providers.OpenAI.ButlerOpenAiProvider;

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

#pragma warning disable OPENAI001
            if (X.ReasoningEffortLevel == ChatReasoningEffortLevel.High)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Max;
            } else if (X.ReasoningEffortLevel ==  ChatReasoningEffortLevel.Medium)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Medium;
            } else if (X.ReasoningEffortLevel == ChatReasoningEffortLevel.Low)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.Low;
            } else if (X.ReasoningEffortLevel ==  ChatReasoningEffortLevel.None)
            {
                ret.ReasoningEffort = ButlerThinkingEffortChoice.None;
            } else if (X.ReasoningEffortLevel == null)
            {
                ret.ReasoningEffort = null;
            }
            else
            {
                throw new InvalidDataException("Unknown reasoning effort. Accepted values are {High, medium, low, none} if openai has added more, this provider for butler needs update");
            }

  

#pragma warning restore OPENAI001
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

#pragma warning disable OPENAI001
            switch (Opts.ReasoningEffort)
            {
                case ButlerThinkingEffortChoice.Max:
                case ButlerThinkingEffortChoice.High:
                    ret.ReasoningEffortLevel = ChatReasoningEffortLevel.High;
                    break;
                case ButlerThinkingEffortChoice.Medium:
                    ret.ReasoningEffortLevel = ChatReasoningEffortLevel.Medium;
                    break;
                case ButlerThinkingEffortChoice.Low:
                    ret.ReasoningEffortLevel = ChatReasoningEffortLevel.Low;
                    break;
                case ButlerThinkingEffortChoice.None:
                    ret.ReasoningEffortLevel = ChatReasoningEffortLevel.None;
                    break;
                case null:
                    break;
                default:
                    throw new InvalidDataException("Unknown reasoning effort. Accepted values are {High, medium, low, none} if openai has added more, this provider for butler needs update");

            }
#pragma warning disable OPENAI001






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
                    continue;
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

    public enum ModelConfig_Type
    {
        /// <summary>
        /// Not set or not existing in the data
        /// </summary>
        Undefined,
        /// <summary>
        /// Use chat completion mode
        /// </summary>
        ChatCompletion = 1,
        /// <summary>
        /// Use reasoning mode
        /// </summary>
        Response = 2

    }

    public interface IButlerOpenAIProviderEngineConfig
    {
        public ConcurrentDictionary<string, ModelConfig_Type> Data { get; }
        /// <summary>
        /// If a model is not in the list, this defines the sub engine we use (<see cref="ModelConfig_Type.ChatCompletion"/> or <see cref="ModelConfig_Type.Response"/>
        /// </summary>
        public ModelConfig_Type DefaultType { get; set; }
        /// <summary>
        /// Wipe the DB so only the <see cref="DefaultType"/> mode is used
        /// </summary>
        public void ClearConfigData();

        /// <summary>
        /// Add the passed model name to use that mode instead of <see cref="DefaultConfig"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="Kind"></param>
        /// <returns>return true if added or false if you don't.</returns>
        public bool AddModelConfig(string name, ModelConfig_Type Kind);

        /// <summary>
        /// lookup the model name in your DB, if not found - return <see cref="DefaultType"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ModelConfig_Type LookupModelConfig(string name);


        /// <summary>
        /// Remoe the passed model name if existant, will use <see cref="DefaultConfig"/>
        /// </summary>
        /// <param name="name"></param>
        public bool RemoveModelConfig(string name);
    }
    

    public class ButlerOpenAiProvider : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProvider_RecoverOptions
    {
        /// <summary>
        /// If null, uses a built in one.
        /// </summary>
        public IButlerOpenAIProviderEngineConfig? ProtocolConfig { get; set; } = null;
        internal class ForwardBranchDB: IButlerOpenAIProviderEngineConfig
        {
            public static ForwardBranchDB Instance = new ForwardBranchDB();
            public ConcurrentDictionary<string, ModelConfig_Type> Data { get => _Data; }
            public ModelConfig_Type DefaultType { get => __DefaultType; set => __DefaultType = value; }

            ModelConfig_Type __DefaultType = ModelConfig_Type.ChatCompletion;
            readonly ConcurrentDictionary<string, ModelConfig_Type> _Data = new ConcurrentDictionary<string, ModelConfig_Type>();


            public bool AddModelConfig(string name, ModelConfig_Type Kind)
            {
                if (Data.TryAdd(name, Kind))
                {
                    return true;
                }
                return false;
            }

            public void ClearConfigData()
            {
                Data.Clear();
            }

            public ModelConfig_Type LookupModelConfig(string name)
            {
                if (Data.TryGetValue(name, out ModelConfig_Type modelConfig))
                {
                    return modelConfig;
                }
                return DefaultType;
            }

            public bool RemoveModelConfig(string name)
            {
                if (Data.TryRemove(name, out var _))
                {
                    return true;
                }
                return false;
            }
        }


      
       


        #region Reasoning Vs ChatCompletion Config



        
        /// <summary>
        /// this by default initals per openai stats essential. if using this provider as shim (ollama, deep, ect...) call <see cref="ClearConfig_Data"/> after <see cref="Initialize(SecureString)"/>
        /// </summary>
        protected virtual void SetupModelConfigDefaults()
        {
            /* the mean diff is that we ant to data sense ssentially
             * astra to go to response instead of the other
             */
            if (this.ProtocolConfig is null)
            {
                this.ProtocolConfig = ForwardBranchDB.Instance;
            }

            ProtocolConfig.AddModelConfig("gpt-6-astra", ModelConfig_Type.Response);




            ProtocolConfig.AddModelConfig("gpt-4o", ModelConfig_Type.ChatCompletion);
        }
        public ModelConfig_Type DefaultConfig => ForwardBranchDB.Instance.DefaultType;

        #endregion
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


            ModelConfig_Type ConnectorMode;

            if (ProtocolConfig is not null)
            {
                ConnectorMode = ProtocolConfig.LookupModelConfig(model);
            }
            else
            {
                ConnectorMode = ModelConfig_Type.ChatCompletion;
            }

            switch (ConnectorMode)
            {
                case ModelConfig_Type.ChatCompletion:
                    {
                        ChatClient? client = OpenAIHandler.GetChatClient(model);
                        if (client is null)
                        {
                            this._factory?.LogError("{Provider} could not find model {model} to create chat client.", nameof(ButlerOpenAiProvider), model);
                            throw new ModuleNotFoundException(model);
                        }
                        if (PPR is null)
                        {
                            PPR = DefaultOpenAiProvider.Instance;
                        }
                        return new ButlerOpenAiChatClient(client, this, PPR);
                    }
                case ModelConfig_Type.Response:
                    {
                        ResponsesClient client = OpenAIHandler.GetResponsesClient();
                        if (client is null)
                        {
                            this._factory?.LogError("{Provider} could not find model {model} to create chat client.", nameof(ButlerOpenAiProvider), model);
                            throw new ModuleNotFoundException(model);
                        }
                        if (PPR is null)
                        {
                            PPR = DefaultOpenAiProvider.Instance;
                        }
                        return new ButlerOpenAiResponseClient(client, this, PPR, model);
                    }
                case ModelConfig_Type.Undefined:
                default:
                    this._factory?.LogInformation("ERROR: UNKNOWN MODEL CONFIG MODE. Check Your OPenai Provider build mode (see the interal class)");
                    throw new InvalidDataException("Error invalid model config");
            }

        }

        public void Initialize(SecureString key)
        {
            this.SetupModelConfigDefaults();
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
    /// Looks like a backend change or (Ollama got more strict) requires smooshing providers. If you don't specifiy
    /// </summary>
    internal class DefaultOpenAiProvider : IButlerChatPreprocessor
    {
        public static readonly DefaultOpenAiProvider Instance = new DefaultOpenAiProvider();
        public IList<ButlerChatMessage> PreprocessMessages(IList<ButlerChatMessage> messages)
        {
            /* so i think i'm gonna take the list at face value 
             * a backend of ollama says you, sys prompts must be message 0.
             * BTw while this is in the openai project, when it works, it's gonna be 
             */
            var ret = new List<ButlerChatMessage>();
            var sys_ret = new List<ButlerChatMessage>();
            for (int i = 0; i < messages.Count;i++)
            {
                if  ( (messages[i] is ButlerSystemChatMessage) || (messages[i].Role == ButlerChatMessageRole.System))
                {
                    sys_ret.Add(messages[i]); 
                }
                else
                {
                    ret.Add(messages[i]);
                }
            }
            if (sys_ret.Count == 0)
            {
                return ret;
            }
            else
            {
                if (sys_ret.Count == 1)
                {
                    ret.Insert(0, sys_ret[0]);
                }
                else
                {
                    string contents = string.Empty;
                    for (int i = 0; i < sys_ret.Count; i++)
                    {
                        contents += sys_ret[i].GetCombinedText();
                    }
                    ButlerSystemChatMessage x = new ButlerSystemChatMessage(contents);
                    x.Role = ButlerChatMessageRole.System;
                    ret.Insert(0, x);
                }
                return ret;
            }
        }
    }




}
