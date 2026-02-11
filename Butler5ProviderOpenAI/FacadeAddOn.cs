using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK;
using ButlerSDK.Core;
using ButlerSDK.Providers.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAiProvider
{
    public static class FacadeAddOn
    {
        static readonly string FacadeKey = "OpenAIGenericKey_FacadeCreate";

        /// <summary>
        /// Create a Butler instance using OpenAI as the LLM provider using default options. 
        /// </summary>
        /// <param name="Self">The this parameter <see cref="ButlerStarter.Instance"/></param>
        /// <param name="OpenAiKey">the key to seed the <see cref="InMemoryApiKey"/> </param>
        /// <param name="Model">modal to attempt loading</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>More power is available directly by <see cref="Butler"/> class directly but requires more setup</remarks>
        public static Butler CreateOpenAiButler(this ButlerStarter Self, string OpenAiKey, string Model)
        {

            ButlerOpenAiProvider provider = new ButlerOpenAiProvider();
            ArgumentException.ThrowIfNullOrWhiteSpace(OpenAiKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(Model);
            InMemoryApiKey CloudKeys = new();
            CloudKeys.AddKey(FacadeKey, OpenAiKey);
            var ret = new Butler(CloudKeys, provider, provider.ChatCreationProvider.DefaultOptions, Model, FacadeKey, null!, null!);
            if (ret == null)
            {
                throw new InvalidOperationException("Failed to create Butler instance with OpenAI provider.");
            }
            return ret;
        }
    }
}
