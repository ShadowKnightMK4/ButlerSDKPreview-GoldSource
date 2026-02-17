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
    /// <summary>
    /// Kinda a serving of a base. Never buffers, always passes, never triggers <see cref="Remedial(TrenchCoatChatCollection, ToolResolver, ButlerToolBench)"/> 
    /// </summary>
    public class PassThruPostProcessing : IButlerPostProcessorHandler
    {
        public int QueueSize => 0;

        /// <summary>
        /// Nothing to remove, Always passes thru, 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ButlerStreamingChatCompletionUpdate? DeQueueBuffer()
        {
            // nothing to remove from the queue
            throw new NotImplementedException();
        }

        
        /// <summary>
        /// Do not trigger <see cref="Remedial(TrenchCoatChatCollection, ToolResolver, ButlerToolBench)"/>
        /// </summary>
        /// <param name="Reason"></param>
        /// <param name="Msgs"></param>
        /// <param name="Assistent"></param>
        /// <param name="Options"></param>
        /// <returns></returns>
        public IButlerPostProcessorHandler.EndOfAiStreamAction EndOfStreamAlert(ButlerChatFinishReason? Reason, IList<ButlerChatMessage> Msgs, ButlerChatMessage Assistent, IButlerChatCompletionOptions Options, bool WasToolCalled)
        {
            return IButlerPostProcessorHandler.EndOfAiStreamAction.None;
        }

        /// <summary>
        /// Discard empty stuff, send the rest
        /// </summary>
        /// <param name="update"></param>
        /// <param name="WasToolTriggered"></param>
        /// <returns></returns>
        public IButlerPostProcessorHandler.PostProcessorAction ProcessReply(ButlerStreamingChatCompletionUpdate? update, bool WasToolTriggered)
        {
            if (update == null)
            {
                return IButlerPostProcessorHandler.PostProcessorAction.Discard;
            }
            else
            {
                return IButlerPostProcessorHandler.PostProcessorAction.PassThru;
            }
        }






        /// <summary>
        /// does nothing (pass thru class)
        /// </summary>
        /// <param name="Msgs"></param>
        /// <param name="Resolver"></param>
        /// <param name="Toolset"></param>
        public void Remedial(IButlerChatCollection Msgs, ToolResolver Resolver, IButlerToolBench Toolset)
        {
            throw new NotImplementedException();
        }
    }
}
