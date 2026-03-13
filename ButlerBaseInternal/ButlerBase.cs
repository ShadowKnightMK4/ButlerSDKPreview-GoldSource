using ButlerBaseInternal;
using ButlerLLMProviderPlatform.Protocol;
using ButlerProtocolBase.ToolSecurity;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ButlerPostProcessing;
using ButlerSDK.Debugging;
using ButlerSDK.ToolSupport;
using ButlerSDK.ToolSupport.Bench;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using ButlerToolContracts.DataTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Xml.Linq;



namespace ButlerSDK.Core
{



   
    /// <summary>
    /// <see cref="ButlerBase"/> Base class for Butler chat session
    /// </summary>
    public abstract class ButlerBase: IButlerChatSession
    {
        /// <summary>
        /// The Required tool scope. <see cref="AddTool(IButlerToolBaseInterface)"/> and by ext <see cref="AddSystemTool(IButlerSystemToolInterface)"/> will reject tools added that request flags not set here. 
        /// </summary>
        /// <remarks>Implementation detail: <see cref="ToolSet"/> and the default <see cref="ButlerToolBench"/> are were that part lives</remarks>
        protected ToolSurfaceScope _MinToolScope = ToolSurfaceScope.StandardReading;

        /// <summary>
        /// The ToolSurfaceScope acts as as gatekeeper. Tool indicates at compile time what it reports to do.
        /// </summary>
        /// <remarks>Do be accurate. A future roadmap is on the horizon where I aim for these to be enforced not just at <see cref="AddTool(IButlerToolBaseInterface)"/> . Note it should be enforced at <see cref="ButlerToolBench"/> / <see cref="IButlerToolBench"/> level</remarks>
        public ToolSurfaceScope ToolSurfaceScope
        {
            get
            {
                return _MinToolScope;
            }
            set
            {
                _MinToolScope = value;
            }
        }
        
        #region Required Interface/ dependent classes
        /// <summary>
        /// Each tool will get a copy of this
        /// </summary>
        IButlerVaultKeyCollection Keys;
        /// <summary>
        /// The LLM provider Butler is using
        /// </summary>
        protected IButlerLLMProvider Provider;
        /// <summary>
        /// THE <see cref="Provider"/> derived Factory reacting chat clients
        /// </summary>
        protected IButlerChatCreationProvider ChatFactory;
        /// <summary>
        /// Current chat client
        /// </summary>
        protected IButlerChatClient? Chat;

        /// <summary>
        /// Assign a <see cref="ButlerTap"/> here to Trace
        /// </summary>
        public ButlerTap? DebugTap;
        #endregion
   

  
        /// <summary>
        /// The chat model used by the last response
        /// </summary>
        public string? ChatModel
        {
            get
            {
                return ChatModel_Internal;
            }
            set
            {
                ChatModel_Internal = value;
            }
        }

        /// <summary>
        /// backing variable for <see cref="ChatModel"/>
        /// </summary>
        internal string? ChatModel_Internal = null;


    
        /// <summary>
        /// Setup the class and use the default chat message container - <see cref="TrenchCoatChatCollection"/>
        /// <summary>
        /// <param name="Keys">This is used to source needed API keys needed.  </param>
        /// <param name="Handler">An implementation of the provider that Butler will use to communicate with the LLM of your choice - see <see cref="IButlerLLMProvider"/> </param>
        /// <param name="Options">This can be null if the provider <see cref="IButlerChatCreationProvider"/> has a <see cref="IButlerChatCreationProvider.DefaultOptions"/> that is not null</param>
        /// <param name="ModelChoice">This is the model requested. It's passed as its and cannot be null </param>
        /// <param name="LLMKEYVAR">Related to <see cref="IButlerVaultKeyCollection"/> Keys instance. Butler will require the key named that.</param>
        /// <exception cref="ArgumentNullException"> can trigger If <see cref="Keys"/>, <see cref="Handler"/> and <see cref="ModelChoice"/> are null </exception>
        
        public ButlerBase(IButlerVaultKeyCollection Keys, IButlerLLMProvider  Handler, IButlerChatCompletionOptions? Options,string ModelChoice,  string LLMKEYVAR= "" )
        {
            ArgumentNullException.ThrowIfNull(Keys, nameof(Keys));
            ArgumentNullException.ThrowIfNull(Handler, nameof(Handler));
            ArgumentNullException.ThrowIfNull(ModelChoice, nameof(ModelChoice));
            ChatModel = ModelChoice;
            if (Options is null)
            {
                Options = Handler.ChatCreationProvider.DefaultOptions;
            }
            
            ArgumentNullException.ThrowIfNull(Options, nameof(Options));
            this.Keys = Keys;
            Provider = Handler;




            InitializeHandler(LLMKEYVAR, new TrenchCoatChatCollection() as IButlerChatCollection);
            _MainOptions = Options;

        }

        /// <summary>
        /// Setup the class and use the default chat message container - <see cref="TrenchCoatChatCollection"/> or provider your own of either <see cref="IButlerChatCollection"/> or <see cref="IButlerTrenchImplementation"/>
        /// </summary>
        /// <param name="Keys">This is used to source needed API keys needed.  </param>
        /// <param name="Handler">An implementation of the provider that Butler will use to communicate with the LLM of your choice - see <see cref="IButlerLLMProvider"/> </param>
        /// <param name="Options">This can be null if the provider <see cref="IButlerChatCreationProvider"/> has a <see cref="IButlerChatCreationProvider.DefaultOptions"/> that is not null</param>
        /// <param name="ModelChoice">This is the model requested. It's passed as its and cannot be null </param>
        /// <param name="LLMKEYVAR">Related to <see cref="IButlerVaultKeyCollection"/> Keys instance. Butler will require the key named that.</param>
        /// <param name="ChatMessageHandler">How Butler stores the chat list. This argument lets you supply your own</param>
        /// <exception cref="ArgumentNullException"> can trigger If <see cref="Keys"/>, <see cref="Handler"/> and <see cref="ModelChoice"/> are null </exception>
        public ButlerBase(IButlerVaultKeyCollection Keys, IButlerLLMProvider Handler, IButlerChatCompletionOptions? Options, string ModelChoice, string LLMKEYVAR = "", IButlerChatCollection? ChatMessageHandler=default)
        {
            ArgumentNullException.ThrowIfNull(Keys, nameof(Keys));
            ArgumentNullException.ThrowIfNull(Handler, nameof(Handler));
            ArgumentNullException.ThrowIfNull(ModelChoice, nameof(ModelChoice));
            ChatModel = ModelChoice;
            if (Options is null)
            {
                Options = Handler.ChatCreationProvider.DefaultOptions;
            }

            ArgumentNullException.ThrowIfNull(Options, nameof(Options));
            this.Keys = Keys;
            Provider = Handler;




   
                InitializeHandler(LLMKEYVAR, ChatMessageHandler);
        
            _MainOptions = Options;

        }
        [MemberNotNull(nameof(ChatFactory))]
        [MemberNotNull(nameof(Chat))]
        [MemberNotNull(nameof(_ChatCollection))]
        
        internal void InitializeHandler(string APIKEYNAME, IButlerChatCollection? ChatStorage)
        {
            
            if (ChatStorage is null)
            {
                this.DebugTap?.LogString($"Setting up Butler instance with default type {nameof(TrenchCoatChatCollection)}\r\n ");
                this._ChatCollection = new TrenchCoatChatCollection();
            }
            else
            {
                this.DebugTap?.LogString($"Setting up Butler instance with default type {ChatStorage.GetType().Name}\r\n ");
                this._ChatCollection = ChatStorage;
            }
            // get the keys. Note execution path from butler5 should mean this is never actually tripped
            if (Keys is null)
            {
                throw new InvalidOperationException("why did GetEndPoints() be called if Key interface not set? - Set that first!");
            }
            if (ChatModel is null)
            {
                throw new InvalidOperationException("Fatal error: Need to specific which model to load in the provider!");
            }
            /*
             * step 1 get the factory, 
             */
            var LocalFactory = Provider.ChatCreationProvider;
            this.ChatFactory = LocalFactory;

            if (ChatFactory is null)
            {
                throw new InvalidOperationException("Error: Provider was unable to give butler a chat factor instance - IButlerChatCreationFactory");
            }

            if (!string.IsNullOrEmpty(APIKEYNAME))
            {
                // and create the common class. OpenAiClient
                using (var NeededKey = Keys.ResolveKey(APIKEYNAME))
                {
                    if (NeededKey is null)
                    {
#if DEBUG
                        throw new InvalidOperationException($"Error: LLM Key requested not found in Key vault store. Ensure a key exists named '{APIKEYNAME}'");
#else
                        throw new InvalidOperationException($"Error: LLM Key requested not found in Key vault store. Ensure a proper key exists -  does the key have a matching name?");
#endif 
                    }
                    try
                    {
                        LocalFactory.Initialize(NeededKey);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Implementation of LLM Interface failed to properly init. The inner exception may be useful", e);
                    }
                }
            }
            else
            {
                if (LocalFactory is IButlerChatCreationProvider_NoApiNeeded NoApiKey)
                {
                    NoApiKey.Initialize();
                }
                else
                {
                    ChatFactory.Initialize(new SecureString());
                }
            }




            Chat = LocalFactory.GetChatClient(ChatModel, null, null);
         
            if (Chat is null)
            {
                throw new InvalidOperationException("Crit failure: Unable to get initial module from chat factory!");
            }
        }
        

        /// <summary>
        /// Assign this to use that model if possible next turn. Does not persist. Null means use <see cref="ChatModel"/>
        /// </summary>
        public string? ModelChoice;

        /// <summary>
        /// When using that Discoverer, be sure to set it to *here* and also <see cref="AddTool(IButlerToolBaseInterface)"/>. Doing so will hook the LLM up to let it discover tools provided its live atm tools
        /// </summary>

        public IButlerTool_Discoverer? TheToolBox = null;
       
        /// <summary>
        /// The <see cref="IButlerLLMProvider"/> specific class for holding <see cref="ButlerChatCompletionOptions"/>
        /// </summary>
        public IButlerChatCompletionOptions MainOptions { get => _MainOptions; }
        /// <summary>
        /// Backing variable for <see cref="MainOptions"/>
        /// </summary>
        IButlerChatCompletionOptions _MainOptions;
        protected IButlerToolBench ToolSet = (IButlerToolBench) new ButlerToolBench();

        /// <summary>
        /// We disposed?
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Default true. What this does after each changing call to add or remove a tool, redo the list we send to open ai.
        /// </summary>
        /// <remarks>If you have multi tools to add and readjust set this to false and then true when done. true means after each add <see cref="RecalcTools"/> is called</remarks>
        public bool AutoUpdateTooList { get; set; } = true;
        



        
        /// <summary>
        /// Normally will not need to call this. updating the tool list triggers a call to this if <see cref="AutoUpdateTooList"/> is true. This will update the LLM specific tool  class that the tools present as.
        /// </summary>
        /// <remarks>If a <see cref="IButlerToolPromptInjection"/> tool is added BUT the underyling is *not* <see cref="IButlerTrenchImplementation"/>, that's when <see cref="TrenchSupport"/> setting comes into play</remarks>
        public void RecalcTools()
        {
            IButlerTrenchImplementation? Trenchy = _ChatCollection as IButlerTrenchImplementation;
            this._MainOptions.Tools.Clear();
            
            Trenchy?.ClearPromptInjections();

            foreach (var toolname in ToolSet.ToolNames)
            {
                var item = ToolSet.GetTool(toolname);
                if (item != null)
                {
                    if (item is not IButlerPassiveTool)
                    {
                        _MainOptions.Tools.Add(item);
                    }
                    if (item is IButlerToolPromptInjection prompt)
                    {
                        if ((Trenchy is not null) || (TrenchSupport != TrenchSupportFallback.Throw))
                        {
                            Trenchy?.AddPromptInjectionMessage(new ButlerSystemChatMessage(prompt.GetToolSystemDirectionText()), prompt);
                        }
                        else
                        {
                            throw new NotSupportedException($"Error: Attempt to use a Prompt Injection tool {item.ToolName}, However the supplied IButlerChatCollection object does not support this action. Does it implement IButlerTrenchImplementation also?");
                        }
                        
                    }
                }
                else
                {
#if DEBUG
                    Debug.Write($"Warning: {toolname} exists as entry in tools but its blank (null) in tool set. Not adding it.");
#endif
                }
            }
           
            
        }

        #region tool CRUD
        #region Adding Tools

        #region Trency vs bare min difference checking and dealing
        public enum TrenchSupportFallback
        {
            /// <summary>
            /// DEFAULT: If the <see cref="ChatCollection"/> is not a <see cref="IButlerTrenchImplementation"/> subclass, we throw if the tool requires <see cref="IButlerToolPostCallInjection"/> or <see cref="IButlerToolPromptInjection"/>
            /// </summary>
            Throw = 0,
            /// <summary>
            /// If the <see cref="ChatCollection"/> is not of <see cref="IButlerTrenchImplementation"/>, a tool that uses <see cref="IButlerToolPromptInjection"/> or <see cref="IButlerToolPostCallInjection"/> registeres BUT the routines will not be called
            /// </summary>
            DisableToolPromptSteering = 1,
            /// <summary>
            /// Do DisableToolPromptSteering and log to the tap
            /// </summary>
            DiscardAndLog = 2
        }


        /// <summary>
        /// backing variable for <see cref="TrenchSupport"/>
        /// </summary>
        private TrenchSupportFallback _TrenchFallback = TrenchSupportFallback.Throw;

        /// <summary>
        /// Choose how this instance of Butler will handle Not Trenchy support (aka PostToolCall, and Temporay message removal)
        /// </summary>
        public TrenchSupportFallback TrenchSupport
        {
            get
            {
                return _TrenchFallback;
            }
            set
            {
                _TrenchFallback = value;
            }
        }

        /// <summary>
        /// Placed in <see cref="AddTool(IButlerToolBaseInterface)"/>
        /// </summary>
        /// <param name="x">tool to check</param>
        /// <exception cref="InvalidOperationException">conditionally thrown depending on <see cref="TrenchSupport"/> and it tool is <see cref="IButlerToolPromptInjection"/> tool</exception>
        internal void ValidateTrenchyToolNeedAndNotify(IButlerToolBaseInterface x)
        {
            bool IsTrenchy = this.ChatCollection is IButlerTrenchImplementation TestThis;
            if (IsTrenchy)
            {
                // we're full power, skip the rest
                return;
            }
            ArgumentNullException.ThrowIfNull(x);
            if (x is IButlerToolPostCallInjection Post)
            {
                if (!IsTrenchy)
                {
                    switch (_TrenchFallback)
                    {
                        case TrenchSupportFallback.Throw:
                            {
                                throw new InvalidOperationException($"Attempt to Add Post Tool call required tool in strict mode without {nameof(IButlerTrenchImplementation)} support. You can disable this by setting {nameof(TrenchSupportFallback)} to an action other than throw. Warning: Adding tools that require Trenchy may trigger malfunction if this flag is set to something not throwing.");
                            }
                        case TrenchSupportFallback.DisableToolPromptSteering:
                            {
                                // do nothing;
                                break;
                            }
                        case TrenchSupportFallback.DiscardAndLog:
                            {
                                DebugTap?.LogString($"Someone added a tool that requires  {nameof(IButlerTrenchImplementation)} support but has not used a chat collection with that. The action is discarded, but the tool may malfunction if continued.");
                                break;
                            }
                    }
                }
            }
            else
            {
                if (!IsTrenchy)
                {
                    if (this.ChatCollection is not IButlerTrenchImplementation)
                    {
                        switch (_TrenchFallback)
                        {
                            case TrenchSupportFallback.Throw:
                                {
                                    throw new InvalidOperationException($"Attempt to Add PromptInjection tool in strict mode without {nameof(IButlerTrenchImplementation)} support. You can disable this by setting {nameof(TrenchSupportFallback)} to an action other than throw. Warning: Adding tools that require Trenchy may trigger malfunction if this flag is set to something not throwing.");
                                }
                            case TrenchSupportFallback.DisableToolPromptSteering:
                                {
                                    // do nothing;
                                    break;
                                }
                            case TrenchSupportFallback.DiscardAndLog:
                                {
                                    DebugTap?.LogString($"Someone added a tool that requires  {nameof(IButlerTrenchImplementation)} support but has not used a chat collection with that. The action is discarded, but the tool may malfunction if continued.");
                                    break;
                                }
                        }
                    }
                }
            }
        }
        
        #endregion
        /// <summary>
        /// If set, invalid names will not trigger an exception by butler4, you may get an exception triggered via OpenAI .net sdk / gemini if your name doesn't follow it's converting of Strictly A-z range, with 0-9 in there. The only allowed symbol is _ and nothing else, not even spaces in the tool name.
        /// </summary>
        /// <remarks>For the default <see cref="ButlerToolBench"/>, see <see cref="ButlerToolBench.ValidateToolName(IButlerToolBaseInterface, bool)"/></remarks>
        public bool AllowUnvalidedToolNames { get; set; }


        /// <summary>
        /// add a tool following the interface
        /// </summary>
        /// <param name="tool">tool to add to the <see cref="MainOptions"/></param>
        /// <exception cref="ArgumentNullException">Thrown if passed tool is null</exception>
        /// <remarks>If you pass <see cref="IButlerSystemToolInterface"/>, this routine will call <see cref="AddSystemTool(IButlerSystemToolInterface)"/> instead</remarks>
        public void AddTool(IButlerToolBaseInterface tool)
        {
            
            ArgumentNullException.ThrowIfNull(tool);
     
            if (tool is IButlerSystemToolInterface sys)
            {
                AddSystemTool(sys);
#if DEBUG
                Debug.WriteLine("Informational: Tried added system tool thru AddTool vs AddSystemTool. This message does not fire in release build");
#endif
                return;
            }

            ValidateTrenchyToolNeedAndNotify(tool);


            if (this.AllowUnvalidedToolNames) 
            {
               ToolSet.AddTool(tool.ToolName, tool, ToolSurfaceScope, false);
            }
            else

            {
                ToolSet.AddTool(tool.ToolName, tool, ToolSurfaceScope, true);
            }

            if (AutoUpdateTooList) RecalcTools();
        }

        
        /// <summary>
        /// Add a system tool and pair both this <see cref="IButlerChatSession"/> and its <see cref="IButlerToolBench"/>
        /// </summary>
        /// <param name="systemTool">the system tool to add</param>
        /// <exception cref="ArgumentNullException">Triggers if the passed tool is null</exception>
        /// <remarks>The main thing a system tool gets is a reference to the supported butler and tool kit - essentially root or kernel level access to butler instance</remarks>
        public void AddSystemTool(IButlerSystemToolInterface systemTool)
        {
            ArgumentNullException.ThrowIfNull(systemTool);
            ValidateTrenchyToolNeedAndNotify(systemTool);
            ToolSurfaceFlagChecking.LookupToolFlag(systemTool, out bool Any, out ToolSurfaceScope Result);
            ToolSet.AddTool(systemTool.ToolName, systemTool, Result, !AllowUnvalidedToolNames);

            systemTool.Pair(this, this.ToolSet);
            // note: unpaired gets triggered on System tool removal
            if (AutoUpdateTooList) RecalcTools();
        }
        #endregion

        #region Searching for Tools
        /// <summary>
        /// Search for a tool by name. returns null if not there.
        /// </summary>
        /// <param name="Name">name to search for</param>
        /// <returns>null if not found or the tool as a <see cref="IButlerToolBaseInterface"/></returns>
        /// <exception cref="ArgumentNullException">This triggers if passed string is null</exception>
        /// <exception cref="ArgumentException">Can trigger if string is empty or white space.</exception>
        public IButlerToolBaseInterface? SearchTool(string Name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            return ToolSet.GetTool(Name);
        }

        /// <summary>
        /// Does this tool exist?
        /// </summary>
        /// <param name="Name">name of the tool to check</param>
        /// <returns>true if it does and false if not</returns>
        /// <exception cref="ArgumentNullException">This triggers if passed string is null</exception>
        /// <exception cref="ArgumentException">Can trigger if string is empty or white space.</exception>
        public bool ExistsTool(string Name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            return ToolSet.ExistsTool(Name);
        }
        #endregion

        #region deleting tools
        /// <summary>
        /// Delete this tool from our list and trigger cleanup if indicated while optionally not removing system tools
        /// </summary>
        /// <param name="name">name of tool</param>
        /// <param name="TriggerCleanup">if true, triggers WindDown and Dispose if implemented</param>
        /// <param name="SkipSystemTools">If Set, tools that are of interface type <see cref="IButlerSystemToolInterface"/> are skipped</param>
        /// <remarks>if you're sharing tool class instances, you may want to set trigger to false BUT the default is true for ease of use</remarks>
        /// <exception cref="ArgumentNullException">This triggers if passed string is null</exception>
        /// <exception cref="ArgumentException">Can trigger if string is empty or white space.</exception>
        public void DeleteTool(string Name, bool TriggerCleanup = true, bool SkipSystemTools = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            ToolSet.RemoveTool(Name, TriggerCleanup, SkipSystemTools);
            if (AutoUpdateTooList) RecalcTools();
        }


        /// <summary>
        /// delate all tools, default triggering clean up and skipping  system tools.
        /// </summary>
        /// <param name="TriggerCleanup">if true, triggers WindDown and Dispose if implemented</param>
        /// <param name="SkipSystemTools">If Set, tools that are of interface type <see cref="IButlerSystemToolInterface"/> are skipped</param>
        public void DeleteAllTools(bool TriggerCleanup = true, bool SkipSystemTools = true)
        {
            ToolSet.RemoveAllTools(TriggerCleanup, SkipSystemTools);
            if (AutoUpdateTooList) RecalcTools();
        }
        #endregion

        #region Replace Tool
        /// <summary>
        /// Change a tool to a new instance.
        /// </summary>
        /// <param name="Name">name of the tool to replace</param>
        /// <param name="NewOne">The tool to swap it out with (or tool instance)</param>
        /// <param name="TriggerCleanup">Trigger cleanup on the old tool?</param>
        /// <remarks>Behind the seens this currently deletes the old tool and adds the new instance</remarks>
        public void UpdateTool(string Name, IButlerToolBaseInterface NewOne, bool TriggerCleanup = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            DeleteTool(Name, TriggerCleanup);
            AddTool(NewOne);
        }
        #endregion
        #endregion


        /// <summary>
        /// This delegate is call back for <see cref="ButlerBase.StreamResponse(ChatMessageStreamHandler, bool)"/> to let you get content as it arrives
        /// </summary>
        /// <param name="content">the next packet of streamed data for you to look at and potentially deal with</param>
        /// <param name="msg">the current message list</param>
        /// <returns>You should return true to keep going and false if you want o bail out</returns>
        public delegate bool ChatMessageStreamHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg);



        /// <summary>
        /// Your subclass should provide your own or in the case of <see cref="Butler"/> in the code does an async flavor and forwards a call here to that.
        /// </summary>
        /// <param name="Handler"></param>
        /// <param name="SkipAddingLLMResponse"></param>
        /// <returns>You s</returns>
        public abstract ButlerChatFinishReason? StreamResponse(ChatMessageStreamHandler Handler, bool SkipAddingLLMResponse = false);


        
        #region How butler does chat for stuff like Maui
        //public readonly FilterdButlerChatCollection ChatCollection = new();
        //public readonly TrenchCoatChatCollection ChatCollection = new();
        protected IButlerChatCollection _ChatCollection;

        /// <summary>
        /// How the chat object is exposed to public use.
        /// </summary>
        public IButlerChatCollection ChatCollection
        {
            get
            {
                return _ChatCollection;
            }
        }

        /// <summary>
        /// Add a user message with text
        /// </summary>
        /// <param name="text">contents of user message</param>
        public void AddUserMessage(string text)
        {
            _ChatCollection.AddUserMessage(text);
        }

        /// <summary>
        /// Add a tool call message with text
        /// </summary>
        /// <param name="CallID">id of the tool call. LLM will likely complain if you have a paired reply. note: <see cref="ToolResolver"/>  will roll one for you if the LLM don't supply one. This routine bypasses that.</param>
        /// <param name="text">contents of the tool call result</param>
        public void AddToolMessage(string CallID, string text)
        {
            _ChatCollection.AddToolMessage(CallID, text);
        }

        public void AddSystemMessage(string text)
        {
            _ChatCollection.AddSystemMessage(text);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.DeleteAllTools(true);
                    this.ToolSet.Dispose();
                    if (Chat is IDisposable disposable) disposable.Dispose();
                    this.Chat = null;
                    this.ChatFactory = null!;
                    this.Keys.Dispose();
                    if (this.TheToolBox is not null)
                    {
                        if (this.TheToolBox is IDisposable cleanup)
                        {
                            cleanup.Dispose();
                        }
                        this.TheToolBox = null;
                    }
                    
                    
                }



                disposedValue = true;
            }
        }

        
         ~ButlerBase()
        {
             // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        
                 Dispose(disposing: false);
         }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion



    }

    
}


