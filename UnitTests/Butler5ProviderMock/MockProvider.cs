


using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Runtime.CompilerServices;
using System.Security;

namespace ButlerSDK.Providers.UnitTesting.MockProvider
{
    
    public class MockProviderEntryPoint : IButlerLLMProvider, IButlerChatCreationProvider, IButlerChatCreationProvider_NoApiNeeded
    {
        public MockProviderEntryPoint()
        {

        }

        public IButlerChatCreationProvider ChatCreationProvider => this;

        public IButlerChatCreationSupportedModels? SupportedModels => throw new NotImplementedException();

        public IButlerChatCompletionOptions DefaultOptions => new ButlerChatCompletionOptions();

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            return butlerToolBase;
        }

        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            return new MockChatClient();
        }

        public void Initialize(SecureString x)
        {
            ;// done;
            WasInitKeyCalled = true;
        }

        public void Initialize()
        {
            ;// done and butler should not be calling the original
            WasIniNOkeyCalled = true;
        }

        public bool WasInitKeyCalled = false;
        public bool WasIniNOkeyCalled = false;
    }

    public class MockChatClient : IButlerChatClient
    {
        public readonly List<ButlerStreamingChatCompletionUpdate> MockReplay = new();

        public IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            throw new NotImplementedException();
        }

        public IButlerCollectionResult<ButlerStreamingChatCompletionUpdate> CompleteChatStreaming(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options)
        {
            // TODO: Alas. Most focus was for the streaming part
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, [EnumeratorCancellation] CancellationToken cancelMe = default)
        {
            foreach (ButlerStreamingChatCompletionUpdate MockPart in MockReplay)
            {
                yield return MockPart;
            }
            yield break;
        }
    }
}