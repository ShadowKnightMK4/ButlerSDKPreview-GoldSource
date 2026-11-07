using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using SecureStringHelper;
using System.Security;

namespace ButlerSDK.Providers.Gemini.Vertex
{
    public class VertexGeminiProvider : IButlerLLMProvider, IButlerLLMProviderToolRequests, IButlerLLMProvider_SpecificToolExecutionPostCall, IButlerChatCreationProvider, IButlerChatCreationProvider_NoApiNeeded
    {
        ButlerGeminiProvider _inner;
        VertexAI? FancyProvider;

        bool IsOwnerOfFancy = false;

        /// <summary>
        /// Setup with the vertex ai object directly.
        /// </summary>
        /// <param name="x"></param>
        /// <remarks>leaks a bid of the abstraction but needed</remarks>
        public VertexGeminiProvider(VertexAI x)
        {
            _inner = new ButlerGeminiProvider(x);
            FancyProvider = x;
            IsOwnerOfFancy = false;
        }

        /// <summary>
        /// Setup with this project, region and access token
        /// </summary>
        /// <param name="project"></param>
        /// <param name="region"></param>
        /// <param name="accessToken"></param>
        public VertexGeminiProvider(string project, string region, string accessToken)
        {
            FancyProvider = new VertexAI(project, region, accessToken);
            IsOwnerOfFancy = true;
            _inner = new ButlerGeminiProvider(FancyProvider);
        }
        /// <summary>
        /// Setup with whatever is the configration settings <see cref="VertexAI"/> will pull
        /// </summary>
        public VertexGeminiProvider()
        {
            _inner = new ButlerGeminiProvider(ButlerGeminiProvider.GoogleEndPointMode.VertexAi);
            FancyProvider = null; // default is fine
        }
        public IButlerChatCreationProvider ChatCreationProvider => this;

        public IButlerChatCreationSupportedModels? SupportedModels => this._inner.SupportedModels;

        public IButlerChatCompletionOptions DefaultOptions => this._inner.DefaultOptions;

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            return _inner.CreateChatTool(butlerToolBase);        }

        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            return _inner.GetChatClient(model, Options, PPR);
        }

        public IButlerLLMProvider.ToolProviderCallBehavior GetToolMode()
        {
            return _inner.GetToolMode();
        }

        public void HandlerToolExecuteMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolResultMessage Item)
        {
            _inner.HandlerToolExecuteMarkup(ProviderSpecific, Item);
        }

        public void HandlerToolExecuteRequestMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolCallMessage Item)
        {
            _inner.HandlerToolExecuteRequestMarkup(ProviderSpecific, Item);
        }

        bool IsInit = false;
        public void Initialize(SecureString x)
        {
            if (!IsInit)
            {
                _inner.Initialize(x);
                IsInit = true;
            }

        }

        /// <summary>
        /// the VertexAI object either runs with it OR jsut let one setup initalization.
        /// </summary>
        public void Initialize()
        {
            if (!IsInit)
            {
                SecureString stub = new SecureString();
                stub.AssignStringThenReadOnly("DUMMYVAL");
                _inner.Initialize(stub);
            }
        }
    }
}
