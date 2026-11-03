using ApiKeyMgr;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Collections.Concurrent;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace ButlerSDK.Providers.TrenchCoat
{
    public class UnsupportedProviderException: Exception
    {
        public UnsupportedProviderException() { }
        public UnsupportedProviderException(string message) : base(message) { }
    }

    public class DuplicateModelEntryException : Exception
    {
        public DuplicateModelEntryException() { }
        public DuplicateModelEntryException(string message) : base(message) { }
    }


    public class TrenchyModelEnum : IButlerChatCreationSupportedModels
    {


        public TrenchyModelEnum(IList<string> ModelList)
        {
            this.ModelName = ModelList;
        }

        public TrenchyModelEnum(List<string> Models)
        {
            this.ModelName = Models;
        }
        IList<string> ModelName;
        public IEnumerable<string> GetEnumerator()
        {
            return ModelName;
        }
    }

    public class ButlerTrenchCoatLLMProvider: ButlerTrenchCoatLLMProviderBase
    {

    }


    
    /// <summary>
    /// Trench coat provider is a forwarding dispatcher to other providers.
    /// </summary>
    public abstract class ButlerTrenchCoatLLMProviderBase : IButlerLLMProvider, IButlerChatCreationProvider, IButlerLLMProvider_RecoverOptions, IButlerLLMProvider_SpecificToolExecutionPostCall
    {
        /// <summary>
        /// Presented name, system
        /// </summary>
       readonly  ConcurrentDictionary<string, ModelEntry> CrossRefEnv = new();


        /// <summary>
        /// used by model resolver to tell the implementation *what* to setup.
        /// </summary>
        public readonly InMemoryApiKey EnvNameFetching = new();
        class ModelEntry
        {
            public IButlerLLMProvider Provider;
            public IButlerChatCreationProvider ChatCreationProvider;
            public string RequestedModel;
            public string APIKEYNAME;
            public bool WasInitCalled = false;
        }
        #region

        /// <summary>
        /// Either adds the model, overriding it or th
        /// </summary>
        /// <param name="Core"></param>
        /// <param name="ChatCreationProvider"></param>
        /// <param name="ModelName"></param>
        /// <param name="APIKEYID"></param>
        /// <param name="PresentedName"></param>
        /// <param name="OverrideIfDuplicate"></param>
        /// <remarks>Return true if ok, and false on error</remarks>
        /// <exception cref="DuplicateModelEntryException"> triggers if the error was an existing model</exception>
        public bool AddModel(IButlerLLMProvider Core, IButlerChatCreationProvider ChatCreationProvider, string ModelName, string APIKEYID, string PresentedName, bool OverrideIfDuplicate=true)
        {
            ModelEntry Entry = new();
            Entry.Provider = Core;
            Entry.ChatCreationProvider = ChatCreationProvider;
            Entry.RequestedModel = ModelName;
            Entry.APIKEYNAME = APIKEYID;

            if (CrossRefEnv.TryGetValue(PresentedName, out var CurrentVal))
            {
                if (OverrideIfDuplicate)
                {
                    if (CrossRefEnv.TryRemove(PresentedName, out var _))
                    {
                        if (CrossRefEnv.TryAdd(PresentedName, Entry))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    throw new DuplicateModelEntryException($"There's already a model under the {PresentedName}. Either call this with OverriderDuplicate true or remove this instance first before adding");
                }
            }
            else
            {
                if (CrossRefEnv.TryAdd(PresentedName, Entry))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public IButlerChatCreationProvider ChatCreationProvider => this;

        public IButlerChatCreationSupportedModels? SupportedModels
        {
            get
            {
                return new TrenchyModelEnum(this.CrossRefEnv.Keys.ToList());
            }
        }

        public IButlerChatCompletionOptions DefaultOptions => new ButlerChatCompletionOptions();

        public object CreateChatTool(IButlerToolBaseInterface butlerToolBase)
        {
            throw new NotImplementedException();
        }

        public IButlerChatClient? GetChatClient(string model, object? Options, IButlerChatPreprocessor? PPR)
        {
            IButlerChatClient? ret;
            if (CrossRefEnv.TryGetValue(model, out var entry))
            {
                if (!entry.WasInitCalled)
                {
                    var key = this.EnvNameFetching.ResolveKey(entry.APIKEYNAME);
                    if (key is null)
                    {
                        throw new InvalidDataException("Error: Unable to get chat client due to not known *whhich* key to load Ensure key exists in db");
                    }
                    else
                    {
                     
                    }
                }
                ret = entry.ChatCreationProvider.GetChatClient(entry.RequestedModel, Options, PPR);
            }
            return null;
        }

        public void HandlerToolExecuteMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolResultMessage Item)
        {
            throw new NotImplementedException();
        }

        public void HandlerToolExecuteRequestMarkup(Dictionary<string, string> ProviderSpecific, ButlerChatToolCallMessage Item)
        {
            throw new NotImplementedException();
        }

        public void Initialize(SecureString x)
        {
            throw new NotImplementedException();
        }

        public bool StreamingErrorHandler(Exception x, bool IsAggregated, out int SleepTime)
        {
            throw new NotImplementedException();
        }
    }


}
