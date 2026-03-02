


using ButlerLLMProviderPlatform.DataTypes;
using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Runtime.CompilerServices;
using System.Security;

/*
 * A Word:
 * The mock provider exists to test the unit tests and should not be assumed to stay
 * the same from version to verison.
 * Please do not base your code on the implementation of the mock provider as it is not
 * meant to be a stable API. It is only meant to be a tool for the unit tests and may change 
 * without notice.
 */
namespace ButlerSDK.Providers.UnitTesting.MockProvider
{
    
    public class MockProviderEntryPoint : IButlerLLMProvider, IButlerChatCreationProvider, IButlerChatCreationProvider_NoApiNeeded, IButlerLLMProvider_RecoverOptions
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

        /// <summary>
        /// This sets what <see cref="GetChatClient(string, object?, IButlerChatPreprocessor?)"/> will return. If swapping, your new chat client should have a 0 argument constructor
        /// </summary>
        public Type ChatClientAward = typeof(MockChatClient);
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

        /// <summary>
        ///  just a pass thru by default
        /// </summary>
        /// <param name="x"></param>
        /// <param name="IsAggregated"></param>
        /// <param name="SleepTime"></param>
        /// <returns></returns>
        public bool StreamingErrorHandler(Exception x, bool IsAggregated, out int SleepTime)
        {
            SleepTime = ErrorHandlerSleepTime;
            return ErrorHandlerReturnValue;
        }

        public int ErrorHandlerSleepTime = 200;
        public bool ErrorHandlerReturnValue = false;

        public bool WasInitKeyCalled = false;
        public bool WasIniNOkeyCalled = false;
    }


    public class ErrorMockClient: MockChatClient
    {
        public class MockException : Exception
        {
            public MockException(string message) : base(message)
            {
            }
        }
        public Exception ExceptionToThrow = new MockException("Mock Exception");

        public new IButlerClientResult CompleteChat(IList<ButlerChatMessage> msg)
        {
            throw new NotImplementedException();
        }

        public new async IAsyncEnumerable<ButlerStreamingChatCompletionUpdate> CompleteChatStreamingAsync(IList<ButlerChatMessage> msg, IButlerChatCompletionOptions options, [EnumeratorCancellation] CancellationToken cancelMe = default)
        {
            throw ExceptionToThrow;
            foreach (ButlerStreamingChatCompletionUpdate MockPart in MockReplay)
            {
                yield return MockPart;
            }
            yield break;
        }
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