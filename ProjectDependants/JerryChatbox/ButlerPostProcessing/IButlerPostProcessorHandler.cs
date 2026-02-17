using ButlerSDK;
using ButlerSDK.ToolSupport;
using ButlerSDK.ToolSupport.Bench;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.ButlerPostProcessing
{
    public interface IButlerPostProcessorHandler
    {
        public enum EndOfAiStreamAction
        {
            /// <summary>
            /// Default to just return control to LLM and butler, typically ending the stream loop
            /// </summary>
            None = 0,
            /// <summary>
            /// Trigger a call to Remedial. 
            /// </summary>
            Triggered = 1,
            /// <summary>
            /// Trigger Remedial and post processor kills its buffer as well as butler's
            /// </summary>
            TriggeredAndDiscard =2,
            /// <summary>
            /// The message the LLM procuded this turn is appended the list as TEMPORARY MESSAGE. This means end of the ai turn will have the message removed.
            /// </summary>
            /// <remarks>What this means for you is that's the final message the ai output before your remedial request. It will be discarded after</remarks>
            TriggeredAndAppendTemp =3,
        }
        public enum PostProcessorAction 
        {
            /// <summary>
            /// Do NOT keep this part
            /// </summary>
            Discard = 0,
            /// <summary>
            /// Call the Handler <see cref="Butler.ChatMessageStreamHandler"/> immediately
            /// </summary>
            PassThru = 1,
            /// <summary>
            /// PostProcessor has buffered this . Do not call Handler
            /// </summary>
            Buffered = 2,
            /// <summary>
            /// PostProessor has buffered this most recent data point, DUMP THE BUFFER to the handler stream now.
            /// </summary>
            DumpBuffer = 3,
            /// <summary>
            /// Discard the current ai turn message being built. Restart with this part
            /// </summary>
            InvalidateAndAppend = 4

        }

        /// <summary>
        /// Butler uses this to pop messages to process you've buffered
        /// </summary>
        /// <returns></returns>
        public ButlerStreamingChatCompletionUpdate? DeQueueBuffer();
        /// <summary>
        /// Should be how big your buffer is
        /// </summary>
        public int QueueSize { get; }

        /// <summary>
        /// Butler calls this to let you have input on what to do with a part
        /// </summary>
        /// <param name="update"></param>
        /// <param name="WasToolTriggered">Caller (<see cref="ButlerSDK.ButlerPostProcessing"/> sets to true it the provider passed a tool call to it</param>
        /// <returns></returns>
        public PostProcessorAction ProcessReply(ButlerStreamingChatCompletionUpdate? update, bool WasToolTriggered);

        /// <summary>
        /// When the LLM reaching a STOP point ie <see cref="ButlerChatFinishReason.Stop"/> (AND NOT TOOL CALL-  butler handle's that itself), this is called
        /// </summary>
        /// <param name="Reason"></param>
        /// <param name="Msgs"></param>
        /// <returns></returns>
        public IButlerPostProcessorHandler.EndOfAiStreamAction EndOfStreamAlert(ButlerChatFinishReason? Reason, IList<ButlerChatMessage> Msgs, ButlerChatMessage Assistent, IButlerChatCompletionOptions Options, bool WasToolCalled);

        /// <summary>
        /// If your <see cref="EndOfAiStreamAction"/> returns <see cref="EndOfAiStreamAction.Triggered"/>, this is called
        /// </summary>
        /// <param name="Msgs">messages after ai turn done, including temporary</param>
        /// <param name="Resolver">the butler too scheduler</param>
        /// <param name="Toolset">the current tool set</param>
        /// <remarks>After calling this, the possible modified data presented to Remedial is resent. ALSO! Any messages you add that you don't want to persist (for example to guide the model, mark <see cref="ButlerChatMessage.IsTemporary"/> to true. They are removed at end of turn</remarks>
        public void Remedial(IButlerChatCollection Msgs, ToolResolver Resolver, IButlerToolBench Toolset);
    }
}
