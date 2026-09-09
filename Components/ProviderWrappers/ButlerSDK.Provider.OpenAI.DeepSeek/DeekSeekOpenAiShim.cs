using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using SecureStringHelper;
using System.Security;

namespace ButlerSDK.Providers.OpenAI.DeepSeek
{
    /// <summary>
    /// A small 'shim' for using the OpenAI Provider to talk to Deek Seek models from its OpenAI end point
    /// </summary>
    public class DeepSeekOpenAiProvider : IButlerLLMProvider,  IButlerChatCreationProvider
    {

        ButlerOpenAiProvider local;
        /// <summary>
        /// DeepSeek default target per documentation
        /// </summary>
        public const string DefaultTarget = "https://api.deepseek.com";
        private const string Message = "The Deep Seek Shim (that depends on OpenAI Provider) did not initialize it's provider ok.";

        /// <summary>
        /// Initialize DeepSeenk provider in OpenAI mode. Set target. Will use <see cref="DefaultTarget"/> if null
        /// </summary>
        /// <param name="Target">If unset, or null, uses <see cref="DefaultTarget"/></param>
        public DeepSeekOpenAiProvider(Uri? Target)
        {
            if (Target is null)
                Target = new Uri(DefaultTarget);
            local = new ButlerOpenAiProvider(Target);

        }
        public IButlerChatCreationProvider ChatCreationProvider => this;

        public IButlerChatCreationSupportedModels? SupportedModels => local.SupportedModels;

        public IButlerChatCompletionOptions DefaultOptions => local.DefaultOptions;

        IButlerChatCompletionOptions IButlerChatCreationProvider.DefaultOptions => local.DefaultOptions;

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            return local.CreateChatTool(butlerToolBase);
        }

        public void Initialize(SecureString key)
        {
            CommmonInitialize(key);
        }

        internal void CommmonInitialize(SecureString? key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            local.Initialize(key);
        }



        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            IButlerChatClient? ret = local.GetChatClient(model, Options, PPR);
            if (ret == null)
            {
                throw new InvalidOperationException(Message);
            }
            else
            {
                return new DeepSeekChatClient(ret);
            }
        }


    }




    /// <summary>
    /// The DeekSeek 'OpenAI' mode Wrapper.  This mostly exists to insert our m in the middle walking class as needed 
    /// </summary>
    public class DeepSeekChatClient : IButlerChatClient
    {


        IButlerChatClient inner;
        public DeepSeekChatClient(IButlerChatClient inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            this.inner = inner;
        }

        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            return inner.CompleteChat(msg);
        }

        /// <summary>
        /// DeekSeek Streaming chat completion. This just wraps the inner streaming result to ensure we have our own enumerator.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            var backbuffer = inner.CompleteChatStreaming(msg, options);
            return new DeepSeekAiChatStreaming(backbuffer);
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

    public class DeepSeekAiChatStreaming : IButlerCollectionResult<ButlerStreamingChatCompletionUpdate>
    {
        private Queue<ButlerStreamingChatCompletionUpdate> queue = new();
        IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> inner;
        public DeepSeekAiChatStreaming(IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> inner)
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
