using ButlerSDK.ApiKeyMgr.Contract;
using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ToolSupport;
using System.Diagnostics;
using System.Security;
using System.Text.Json;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Diagnostics.CodeAnalysis;
using ButlerSDK.Debugging;
using ButlerSDK.ButlerPostProcessing;
using System.Reflection;



namespace ButlerSDK.Core
{



   
    /// <summary>
    /// <see cref="ButlerBase"/> Base class for Butler chat session
    /// </summary>
    public abstract class ButlerBase: IButlerChatSession
    {
        protected ToolSurfaceScope _MinToolScope = ToolSurfaceScope.StandardReading;

        /// <summary>
        /// The ToolSurfaceScope acts as as gatekeeper. Tool indicates at compile time what it reports to do.
        /// </summary>
        /// <remarks>Do be accurate. a future roadmap is on the horizon where I aim for these to be enforced.</remarks>
        public ToolSurfaceScope ToolSurfaceScope => _MinToolScope;
        
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

        internal string? ChatModel_Internal = null;

        /// <summary>
        /// Setup the class
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




            InitializeHandler(LLMKEYVAR);
            _MainOptions = Options;
        }

        [MemberNotNull(nameof(ChatFactory))]
        [MemberNotNull(nameof(Chat))]
        
        void InitializeHandler(string APIKEYNAME)
        {
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
                if (LocalFactory is IButlerChatCreationProvider_NoApiNeeded noapikey)
                {
                    noapikey.Initialize();
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
        /// When using that tool, be sure to set it to *here* and also <see cref="AddTool(IButlerToolBaseInterface)"/>. Doing so will hook the LLM up to let it discover tools provided its live atm tools
        /// </summary>

        public ButlerSDK.ToolSupport.DiscoverTool.ButlerTool_Discoverer? TheToolBox = null;
       
        public IButlerChatCompletionOptions MainOptions { get => _MainOptions; }
        IButlerChatCompletionOptions _MainOptions;
        protected ButlerToolBench ToolSet = new();
        private bool disposedValue;

        /// <summary>
        /// Default true. What this does after each changing call to add or remove a tool, redo the list we send to open ai.
        /// 
        /// </summary>
        public bool AutoUpdateTooList { get; set; } = true;
        



        
        /// <summary>
        /// Normally will not need to call this. updating the tool list triggers a call to this if <see cref="AutoUpdateTooList"/> is true. This will update the LLM specific tool  class that the tools present as.
        /// </summary>
        public void RecalcTools()
        {
            this._MainOptions.Tools.Clear();
            this.ChatCollection.ClearPromptInjections();

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
                        this.ChatCollection.AddPromptInjectionMessage(new ButlerSystemChatMessage(prompt.GetToolSystemDirectionText()), prompt);
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

        /// <summary>
        /// IF set, invalid names will not trigger an exception by butler4, you may get an exception triggered via OpenAI .net sdk if your name doesn't follow it's converting of Strictly A-z range, with 0-9 in there. The only allowed symbol is _ and nothing else, not even spaces in the tool name.
        /// </summary>
        public bool AllowUnvalidedToolNames { get; set; }
        /// <summary>
        /// Add a tool base off the abstract base class
        /// </summary>
        /// <param name="tool"></param>
        public void AddTool(ButlerToolBase tool)
        {
            AddTool(tool as IButlerToolBaseInterface);
        }

        /// <summary>
        /// add a tool following the interface
        /// </summary>
        /// <param name="tool"></param>
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
            if (this.AllowUnvalidedToolNames) 
            {
               ToolSet.AddTool(tool.ToolName, tool, false, ToolSurfaceScope);
            }
            else
            {
                ToolSet.AddTool(tool.ToolName, tool, true, ToolSurfaceScope);
            }

            if (AutoUpdateTooList) RecalcTools();
        }

        
        public void AddSystemTool(IButlerSystemToolInterface systemTool)
        {
            ArgumentNullException.ThrowIfNull(systemTool);
            ToolSet.AddTool(systemTool.ToolName, systemTool, !AllowUnvalidedToolNames);

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
        /// <returns>null if not found or the tool as a IButlerToolBaseInterface</returns>
        public IButlerToolBaseInterface? SearchTool(string Name)
        {
            ArgumentNullException.ThrowIfNull(Name);
            return ToolSet.GetTool(Name);
        }

        /// <summary>
        /// Does this tool exist
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool ExistsTool(string Name)
        {
            ArgumentNullException.ThrowIfNull(Name);
            return ToolSet.ExistsTool(Name);
        }
        #endregion

        #region deleting tools
        /// <summary>
        /// Delete this tool from our list and trigger cleanup if indicated.
        /// </summary>
        /// <param name="name">name of tool</param>
        /// <param name="TriggerCleanup">if true, triggers WindDown and Dispose if implemented</param>
        /// <param name="SkipSystemTools">If Set, tools that are of interface type <see cref="IButlerSystemToolInterface"/> are skipped</param>
        /// <remarks>if you're sharing tool class instances, you may want to set trigger to false BUT the default is true for ease of use</remarks>
        public void DeleteTool(string name, bool TriggerCleanup = true, bool SkipSystemTools = true)
        {
            ArgumentNullException.ThrowIfNull(name);
            ToolSet.RemoveTool(name, TriggerCleanup, SkipSystemTools);
            if (AutoUpdateTooList) RecalcTools();
        }


        /// <summary>
        /// delate all tools
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
        /// way to functionally update a tool in list with new one
        /// </summary>
        /// <param name="name">name of the tool to replace</param>
        /// <param name="NewOne">The tool to swap it out with (or tool instance)</param>
        /// <param name="TriggerCleanup">Trigger cleanup on the old tool?</param>
        public void UpdateTool(string name, IButlerToolBaseInterface NewOne, bool TriggerCleanup = true)
        {
            ArgumentNullException.ThrowIfNull(name);
            DeleteTool(name, TriggerCleanup);
            AddTool(NewOne);
        }
        #endregion
        #endregion


        /// <summary>
        /// This delegate is call back for <see cref="ButlerBase.StreamResponse(ChatMessageStreamHandler, bool)"/> to let you get content as it arrives
        /// </summary>
        /// <param name="content"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public delegate bool ChatMessageStreamHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg);



        public abstract ButlerChatFinishReason? StreamResponse(ChatMessageStreamHandler Handler, bool SkipAddingLLMResponse = false);


        #region How butler does chat for stuff like Maui
        //public readonly FilterdButlerChatCollection ChatCollection = new();
        public readonly TrenchCoatChatCollection ChatCollection = new();

        /// <summary>
        /// Add a user message with text
        /// </summary>
        /// <param name="text">contents of user message</param>
        public void AddUserMessage(string text)
        {
            ChatCollection.AddUserMessage(text);
        }

        /// <summary>
        /// Add a tool call message with text
        /// </summary>
        /// <param name="CallID">id of the tool call. LLM will likely complain if you have a paired reply. note: <see cref="ToolResolver"/>  will roll one for you if the LLM don't supply one. This routine bypasses that.</param>
        /// <param name="text">contents of the tool call result</param>
        public void AddToolMessage(string CallID, string text)
        {
            ChatCollection.AddToolMessage(CallID, text);
        }

        public void AddSystemMessage(string text)
        {
            ChatCollection.AddSystemMessage(text);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.DeleteAllTools(true);
                    this.ToolSet.Dispose();
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


