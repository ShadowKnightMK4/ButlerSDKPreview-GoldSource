using ButlerLLMProviderPlatform;
using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using Microsoft.VisualBasic;
using OpenAI.Chat;
using SecureStringHelper;
using System.ClientModel;
using System.Security;

namespace ButlerSDK.Providers.OpenAI.Ollama
{
    /// <summary>
    /// A small 'shim' for using the OpenAI Provider to talk to Ollama models (IN OPENAI MODE)
    /// </summary>
    public class OllamaOpenAiProvider : IButlerLLMProvider, IButlerChatCreationProvider_NoApiNeeded, IButlerChatCreationProvider
    {

        ButlerOpenAiProvider local;
        /// <summary>
        /// Ollama default target per documentation
        /// </summary>
        public const string DefaultTarget = "http://localhost:11434/v1/";
        /// <summary>
        /// Initialize Ollama provider in OpenAI mode. Set target. Will use <see cref="DefaultTarget"/> if null
        /// </summary>
        /// <param name="Target">If unset, or null, uses <see cref="DefaultTarget"/></param>
        public OllamaOpenAiProvider(Uri? Target)
        {
            if (Target is null)
                Target = new Uri(DefaultTarget);
            local = new ButlerOpenAiProvider(Target);
      
        }
        public IButlerChatCreationProvider ChatCreationProvider => this;

        public IButlerChatCreationSupportedModels? SupportedModels => local.SupportedModels;

        public IButlerChatCompletionOptions DefaultOptions => local.DefaultOptions;

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            return local.CreateChatTool(butlerToolBase);
        }

        public void Initialize(SecureString key)
        {
            CommmonInitialize(key);
            if ( (key is null) || (key.Length == 0))
            {
                Initialize();
                return;
            }
            local.Initialize(key);
        }

        internal void CommmonInitialize(SecureString? key)
        {
            SecureString useKey;
            if  ( (key is null) || (key.Length == 0))
            {
                useKey = new SecureString();
                useKey.AssignStringThenReadyOnly("LLAMA_API_KEY_NOT_NEEDED");
            }
            else
            {
                useKey = key;
            }
            try
            {
                local.Initialize(useKey);
            }
            finally
            {
                if (useKey != key)
                    useKey.Dispose();
            }
        }
        public void Initialize()
        {
            CommmonInitialize(null);
            /// <summary>
            /// While Ollama may or may not need an API  key, The underlying OpenAi LLM Provider *DOES* as well as the OpenAI object we use to talk to Ollama. This key is passed if you don't.
            /// </summary>
            using (SecureString DefaultOllamaKey = new SecureString())
            {
                DefaultOllamaKey.AssignStringThenReadyOnly("LLAMA_API_KEY_NOT_NEEDED");
                local.Initialize(DefaultOllamaKey);
            }
        }

        
        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            IButlerChatClient? ret = local.GetChatClient(model, Options, PPR);
            if (ret == null)
            {
                throw new InvalidOperationException("The Ollama Shim (that depends on Openai Provider) did not initalize it's provider  ok");
            }
            else
            {
                return new OllamaAiChatClient(ret);
            }
        }

    }




    /// <summary>
    /// The Ollama 'OpenAI' mode Wrapper.  This mostly exists to insert our m in the middle walking class for detecting tool call that Ollama don't.  
    /// </summary>
    public class OllamaAiChatClient: IButlerChatClient
    {


        IButlerChatClient inner;
        public OllamaAiChatClient(IButlerChatClient inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            this.inner = inner;
        }

        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            return inner.CompleteChat(msg);
        }

        /// <summary>
        /// Ollama Streaming chat completion. This just wraps the inner streaming result to ensure we have our own enumerator.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            var backbuffer = inner.CompleteChatStreaming(msg, options);
            return new OllamaAiChatStreaming(backbuffer);
        }

        public IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            return inner.CompleteChatStreamingAsync(msg, options);  
        }

        public IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, CancellationToken cancelMe = default)
        {
            return inner.CompleteChatStreamingAsync(msg, options, cancelMe);
        }
    }

    public class OllamaAiChatStreaming: IButlerCollectionResult<ButlerStreamingChatCompletionUpdate>
    {
        private Queue<ButlerStreamingChatCompletionUpdate> queue = new();
        IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> inner;
        public OllamaAiChatStreaming(IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            this.inner = inner;
        }


 

        public IEnumerator<ButlerStreamingChatCompletionUpdate> GetEnumerator()
        {
            foreach (var item in inner)
            {
                yield return item;
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
