using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Provider.Gemini;
using ButlerSDK.Providers.Gemini;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
namespace ButlerSDK.Provider.Gemini.Vertex
{
    
    public class VertexButlerGeminiProvider : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProviderToolRequests, IButlerLLMProvider_SpecificToolExecutionPostCall       
    {
        ButlerGeminiProvider local;

        public VertexButlerGeminiProvider(VertexAI x)
        {
            local = new ButlerGeminiProvider(x);
        }

        public VertexButlerGeminiProvider()
        {
            local = new ButlerGeminiProvider(ButlerGeminiProvider.GoogleEndPointMode.VertexAi);
        }

        /// <summary>
        /// Get your interface that will feed butler a <see cref="IButlerChatCreationProvider"/>
        /// </summary>
        public IButlerChatCreationProvider ChatCreationProvider { get { return this as IButlerChatCreationProvider; } }

        /// <summary>
        /// When given a tool,  create your provider specific object and return it. For Example if you're making an OpenAI based one ChatTool is what your implementation should return a ChatTool instance
        /// </summary>
        /// <param name="butlerToolBase"></param>
        /// <returns>return your provide specific object representing a chat tool.</returns>
        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            return local.CreateChatTool(butlerToolBase);
        }


        /// <summary>
        /// Butler calls this with a string and an TDB options object.
        /// </summary>
        /// <param name="model">model to request from the provider</param>
        /// <param name="Options">chat creation options</param>
        /// <param name="PPR">If non null, the chat should call this interface first before normal translation</param>
        /// <returns>return a provider specific chat client</returns>
        /// <remarks>If you're mentally linking this to OpenAI c# classes, it should return the ChatClient object</remarks>
        /// <exception cref="ModuleNotFoundException">You should throw this if the module requested isn't available</exception>
        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            return local.GetChatClient(model, Options, PPR);
        }

        /// <summary>
        /// Optional. Return null if you don't want nor need to offer a list of supported models..(for example using a custom provider with only one  model)
        /// </summary>
        public IButlerChatCreationSupportedModels? SupportedModels
        {
            get
            {
                return local.SupportedModels;
            }
        }
        /// <summary>
        /// VertexGeminiProvider treats 
        /// </summary>
        /// <param name="x"></param>
        public void Initialize(SecureString x)
        {
            local.Initialize(x);
        }

        /// <summary>
        /// This should return your default chat options object.
        /// </summary>
        public IButlerChatCompletionOptions DefaultOptions { get; }

        public IButlerLLMProvider.ToolProviderCallBehavior GetToolMode()
        {
            return local.GetToolMode();
        }

        
        public void HandlerToolExecuteRequestMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolCallMessage Item)
        {
            local.HandlerToolExecuteRequestMarkup(ProviderSpecific, Item);
        }
        public void HandlerToolExecuteMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolResultMessage Item)
        {
            local.HandlerToolExecuteMarkup(ProviderSpecific, Item);
        }
        
    }

}


