using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.Providers.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Providers.OpenAI.Generic
{
    /// <summary>
    /// A variaent of <see cref="ButlerOpenAiProvider"/> that instructs the pproider to default to Chat Completion instead of the new response support
    /// </summary>
    public class OpenAiGenericProvider: ButlerOpenAiProvider
    {
        public OpenAiGenericProvider(Uri Target) : base(Target, null, null)
        {
        }

        public OpenAiGenericProvider():base(null, null)
        {

        }


        public OpenAiGenericProvider(Uri? EndPoint, ILogger<IButlerLLMProvider>? Logging = null, ILoggerFactory? LogFactory = null): base(EndPoint, Logging, LogFactory)
        {

        }



        protected override void SetupModelConfigDefaults()
        {
            base.SetupModelConfigDefaults();
            if (ProtocolConfig is not null)
            {
                ProtocolConfig.ClearConfigData();
                ProtocolConfig.DefaultType = ModelConfig_Type.ChatCompletion;
            }
        }
    }
}
