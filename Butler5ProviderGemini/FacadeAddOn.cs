using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK;
using ButlerSDK.Core;
using ButlerSDK.Providers.Gemini;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Providers.Gemini
{
    public static class FacadeAddOn
    {
        static readonly string FacadeKey = "GeminiGenericKey_FacadeCreate";
        public static Butler CreateGeminiButler(this ButlerStarter Self, string GeminiKey, string Model)
        {

            ButlerGeminiProvider provider = new ButlerGeminiProvider();
            ArgumentException.ThrowIfNullOrWhiteSpace(GeminiKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(Model);
            InMemoryApiKey CloudKeys = new();
            CloudKeys.AddKey(FacadeKey, GeminiKey);
            var ret = new Butler(CloudKeys, provider, provider.ChatCreationProvider.DefaultOptions, Model, FacadeKey, null!, null!);
            
            if (ret == null)
            {
                throw new InvalidOperationException("Failed to create Butler instance with GeminiProvider provider.");
            }
            return ret;
        }
    }
}
