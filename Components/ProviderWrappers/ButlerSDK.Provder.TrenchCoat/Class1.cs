using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Collections.Concurrent;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace ButlerSDK.Provider.TrenchCoat
{
    public class UnsupportedProviderException: Exception
    {
        public UnsupportedProviderException() { }
        public UnsupportedProviderException(string message) : base(message) { }
    }
    /// <summary>
    /// Trench coat provider is a forwarding dispatcher to other providers.
    /// </summary>
    public abstract class ButlerTrenchCoatLLMProviderBase : IButlerLLMProvider, IButlerChatCreationProvider
    {
        public class ModelEntry
        {
            /// <summary>
            /// Name we expose the model as
            /// </summary>
            public string ExposedName;

            /// <summary>
            /// Name as defined in the provider holding this model
            /// </summary>
            public string ProviderName;
            /// <summary>
            /// Where we go fetch the model
            /// </summary>
            public IButlerLLMProvider Origin;
            public IButlerVaultKeyCollection Keys;
        }


        public readonly ConcurrentDictionary<string, ModelEntry> Entries = new();

        public delegate string IngestProviderModelNameFixup(string ModelName);

        public abstract void IngestProviderModel(IButlerLLMProvider provider, IngestProviderModelNameFixup? Fixup=null);


        public IButlerChatCreationProvider ChatCreationProvider
        {
            get => this;
        }

        public IButlerChatCreationSupportedModels? SupportedModels
        {
            get
            {
                var WalkMe = this.Entries.Keys;
                return new CoatLLMModelEnum(WalkMe);
            }
        }

        public IButlerChatCompletionOptions DefaultOptions => new ButlerChatCompletionOptions();

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            // hmm. what do do here. 
            // but the current assumed standard is essential OPENAI json tool description.
            throw new NotImplementedException();
        }

        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            if (Entries.TryGetValue(model, out ModelEntry? Data))
            {
                if (Data is not null)
                {
                    return Data.Origin.ChatCreationProvider.GetChatClient(Data.ProviderName, Options, PPR);
                }
            }
            return null;
        }

        public void Initialize(SecureString x)
        {
            // we are taking the chat session stance. The other providers need to be init() first for now.
        }
    }



    public class ButlerTrenchCoatLLMProvider : ButlerTrenchCoatLLMProviderBase
    {
        public override void IngestProviderModel(IButlerLLMProvider provider, IngestProviderModelNameFixup? Fixup=null)
        {
            var Factory = provider.ChatCreationProvider;

            if (Factory is null)
            {
                throw new UnsupportedProviderException("Invalid Provider. This Provider does not support the chat factor standard. To Fix: Ensure that it's ChatCreationProvider returns a thing to make chats!");
            }

            if (Factory.SupportedModels is null)
            {
                throw new UnsupportedProviderException($"Unsupported Provider. This provider does not implement model enumeration, a required feature for {nameof(ButlerTrenchCoatLLMProvider)} disptcher provider");
            }
            var WalkMe = Factory.SupportedModels.GetEnumerator();

            if (WalkMe is null)
            {
                throw new UnsupportedProviderException($"Unsupported Provider. This provider does not implement model enumeration, a required feature for {nameof(ButlerTrenchCoatLLMProvider)} disptcher provider. Also the enumerator returned null");
            }

            foreach (string model in WalkMe)
            {
                ModelEntry x = new ModelEntry();
                x.Origin = provider;
                x.ProviderName = model;
                if (Fixup is not null)
                {
                    x.ExposedName = Fixup(x.ProviderName);
                }
                else
                {
                    x.ExposedName = model;
                }
                x.Keys = null;
                this.Entries.TryAdd(x.ExposedName, x);
            }
        }

        
    }

    public class CoatLLMModelEnum : IButlerChatCreationSupportedModels
    {
        ICollection<string> Keys;
        public CoatLLMModelEnum(ICollection<string> Data)
        {
            Keys = Data;
        }
        public IEnumerable<string> GetEnumerator()
        {
            foreach (string entry in Keys)
            {
                yield return entry;
            }
        }
    }



}
