using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using GenerativeAI;
using System.Security;

namespace ButlerSDK.Providers.Gemini.Vertex
{
    public class VertexGeminiProvider : IButlerLLMProvider, IButlerLLMProviderToolRequests, IButlerLLMProvider_SpecificToolExecutionPostCall, IButlerChatCreationProvider
    {
        ButlerGeminiProvider _inner;
        VertexAI? FancyProvider;

        bool IsOwnerOfFancy = false;
        public VertexGeminiProvider(VertexAI x)
        {
            _inner = new ButlerGeminiProvider(x);
            FancyProvider = x;
            IsOwnerOfFancy = false;
        }

        public VertexGeminiProvider(string project, string region, string accessToken)
        {
            FancyProvider = new VertexAI(project, region, accessToken);
            IsOwnerOfFancy = true;
            _inner = new ButlerGeminiProvider(FancyProvider);
        }
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

        public void Initialize(SecureString x)
        {
            _inner.Initialize(x);
        }
    }
}
