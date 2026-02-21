using ButlerLLMProviderPlatform.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using ButlerToolContract;
using ButlerToolContract.DataTypes;


namespace ButlerLLMProviderPlatform.Protocol
{

    /// <summary>
    /// The lynch pin. <see cref="Butler5"/> will be querying this to deal with stuff.
    /// </summary>
    public interface IButlerLLMProvider
    {
        /// <summary>
        /// Get your interface that will feed butler a <see cref="IButlerChatCreationProvider"/>
        /// </summary>
        public IButlerChatCreationProvider ChatCreationProvider { get; }

        /// <summary>
        /// When given a tool,  create your provider specific object and return it. For Example if you're making an OpenAI based one ChatTool is what your implementation should return a ChatTool instance
        /// </summary>
        /// <param name="butlerToolBase"></param>
        /// <returns>return your provide specific object representing a chat tool.</returns>
        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase);

        public enum ToolProviderCallBehavior
        {
            StreamAccumulation = default, // aka OpenAI flavor we collect tool stream data packets and combine them
            OneShot = 1//  aka the provider even in stream mode fires a single tool request off as we get from stream request
        }
    }

    /// <summary>
    /// Have your <see cref="IButlerLLMProvider"/> implement this interface too to gain some fallback options
    /// </summary>
    public interface IButlerLLMProvider_RecoverOptions
    {
        /// <summary>
        /// Control how Butler will return to exceptions your provider throws when attempting to stream.
        /// </summary>
        /// <param name="x">This is the exception that triggered when attempting <see cref="IButlerChatClient.CompleteChatStreaming(IList{ButlerChatMessage}, IButlerChatCompletionOptions)"/> was called
        /// <paramref name="IsAggregated"/>if true then the containing exception is <see cref="AggregateException"/> otherwise it's likely exceptions from attempting to ask your LLM to stream 
        /// <param name="SleepTime">You set this to decide how many ms to sleep if any. This sleep is ignored if you are returning false</param>
        /// <returns>Your provider should return true to sleep and try again or false if the exception is not temporary. Additionally, your routine's objective is to decide if the error is recoverable and how long to sleep if any. Butler's code will figure the rest </returns>
        public bool StreamingErrorHandler(Exception x, bool IsAggregated, out int SleepTime);
    }

    /// <summary>
    /// Optional to control Butler streaming action. If you don't implement it, default is treat as <see cref="IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation"/>
    /// </summary>
    public interface IButlerLLMProviderToolRequests
    {
        public IButlerLLMProvider.ToolProviderCallBehavior GetToolMode();
    }
    /// <summary>
    /// TODOL By default, if you implement this, Butler5 will call this *instead* of <see cref="IButlerChatCreationProvider.Initialize(SecureString)"/> if an empty key is passed for it to load
    /// </summary>
    public interface IButlerChatCreationProvider_NoApiNeeded
    {
        public void Initialize();
    }

    /// <summary>
    /// Mainly for the llama quirky models. This accepts a  list of messages and returns a potentially altered one
    /// </summary>
    public interface IButlerChatPreprocessor
    {
        /// <summary>
        /// Accept a list of stuff to adjust, return *new* copy of the list with your changes.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public IList<ButlerChatMessage> PreprocessMessages(IList<ButlerChatMessage> messages);
    }
    /// <summary>
    /// When given a model to load and an options object, procedure a Generic ChatClient Object that will create the chat option.
    /// </summary>
    /// <remarks>Butler will be passed a class that does this</remarks>
    public interface IButlerChatCreationProvider
    {
        /// <summary>
        /// Butler5 calls this with a string and an TDB options object.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="Options"></param>
        /// <param name="PPR">If non null, the chat should call this interface first before normal translation</param>
        /// <returns></returns>
        /// <remarks>If you're mentally linking this to OpenAI c# classes, it should return the ChatClient object</remarks>
        /// <exception cref="ModuleNotFoundException">You should throw this if the module requested isn't available</exception>
        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR);

        /// <summary>
        /// Optional. Return null if you don't want nor need to offer a list of supported models..(for example using a custom provider with only one  model)
        /// </summary>
        public IButlerChatCreationSupportedModels? SupportedModels { get; }
        /// <summary>
        /// Before calling GetChatClient, Butler5 will call this to initialize your provider with a provided key.
        /// </summary>
        /// <param name="x"></param>
        public void Initialize(SecureString x);

        public IButlerChatCompletionOptions DefaultOptions { get; }
    }

    public interface IButlerChatCreationSupportedModels
    {
        public IEnumerable<string> GetEnumerator();
    }
    /// <summary>
    /// This is what Butler5 calls to get your LLM chat response's. 
    /// </summary>
    public interface IButlerChatClient
    {

        /// <summary>
        /// Butler5 will call this typically when asking for non streaming chats. 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg);
        /// <summary>
        /// Butler5 will call this repeatedly to get get the next part of your chat
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options);

        /// <summary>
        /// The async version of above.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="options"></param>
        /// <param name="cancelMe"></param>
        /// <returns></returns>
        public IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, CancellationToken cancelMe=default);
    }



}
