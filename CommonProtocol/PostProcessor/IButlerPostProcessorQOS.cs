using ButlerLLMProviderPlatform.Protocol;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;

namespace ButlerSDK.ButlerPostProcessing
{
    public interface IButlerPostProcessorQOS: IButlerPostProcessorHandler
    {
        /// <summary>
        /// If your value is false returned, <see cref="ButlerSDK.Core.Butler"/> skips the call
        /// </summary>
        public bool QosEnabled { get; set; } 
        /// <summary>
        /// Optional: Your code gets called after the rest of <see cref="ButlerSDK.Core.ButlerPostProcessing.StreamResponseAsync(ButlerSDK.Core.ButlerBase.ChatMessageStreamHandler, IButlerPostProcessorHandler?, bool, int, CancellationToken)"/> finishes.
        /// </summary>
        /// <param name="Messages">The slice of the context window starting from user turn at [0], to the proposed reply at the end of the list plus </param>
        /// <returns>If you return null, the LLM turn is fully over. If not, *This* message is swapped</returns>
        public Task<ButlerAssistantChatMessage?> FinalQOSCheck(IButlerLLMProvider Prov, IButlerChatClient QOSCheck, IButlerChatCollection Messages, int LastUserMessageIndex, int LastAiTurnIndex);
    }
}
