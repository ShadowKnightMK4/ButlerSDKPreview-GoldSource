using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ButlerPostProcessing;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Core
{
    /* Notice: this is effectively a debug only private class WITH NO GUARENTEE of public api (or code betwen) calls)*/
    public class DebugButler : Butler
    {
        public DebugButler(IButlerVaultKeyCollection Key, IButlerLLMProvider Provider, IButlerChatCompletionOptions? Opts, string ModelChoice, string KeyVar, IButlerPostProcessorHandler? PostProcessor = null, IButlerChatPreprocessor? PPR = null) : base(Key, Provider, Opts, ModelChoice, KeyVar, PostProcessor, PPR)
        {
        }

      //  public new async Task<ButlerChatFinishReason?> StreamResponseAsync(ChatMessageStreamHandler Handler, IButlerPostProcessorHandler? CounterMeasures = null, bool SkipAddingLLMResponse = false, int MaxRemedials = 5, int NetworkErrorMaxRetries = 5, CancellationToken cancelMe = default)
//        {

  //      }
    }
}
