
using System;
using System.Text.Json;
using ButlerToolContract.DataTypes;
namespace ButlerToolContract
{


        /// <summary>
        /// This is thrown by <see cref="Butler.AddTool(IButlerToolBaseInterface)"/> if the added tool name isn't A-z or 0-9 or _ symbol with NO spaces and NOTHING else. You  can disable it by Setting <see cref="Butler4.valid"/>
        /// </summary>
        public class InvalidToolNameException : Exception
        {

            /* bit of history saw the tool name passed to OpenAi is checked with regex. Copied that specific regex from the error message and I added checks at the name validator and by extension butler4 to note if a tool fails before it gets to OpenAi*/
            public InvalidToolNameException() { }
            public InvalidToolNameException(string message) : base(message) { }

            public InvalidToolNameException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }

    /// <summary>
    /// If the platform check fails, this should be thrown by the code that was called to add tool. 
    /// </summary>
    public class PlatformPassFailureException: Exception
    {
        /// <summary>
        /// Default builder
        /// </summary>
        /// <param name="ToolMessage">what the tool reported to help</param>
        /// <param name="ToolName">its name</param>
        /// <returns></returns>
        public static PlatformPassFailureException DefaultBuilder (string ToolMessage, string ToolName)
        {
            if (ToolMessage.Length > 256)
            {
                ToolMessage = ToolMessage.Substring(0, 256);
            }

            return new PlatformPassFailureException($"Tool {ToolName} noted it does not support this platform. Additional message: {ToolMessage}...");
        }
        public PlatformPassFailureException() { }
        public PlatformPassFailureException(string? message) : base(message) { }
        public PlatformPassFailureException(string message, Exception? innerException) : base(message, innerException) { }
    }

        /// <summary>
        /// Scope is before <see cref="IButlerToolSpinup"/>, this will be called. Should that fail, the whole tool is skipped 
        /// </summary>
        public interface IButlerToolPlatformPass
        {
            /// <summary>
            /// do what you need to check if your platform matches. return true if ok. For future exception <see cref="ButlerTool_DeviceAPI_ExternProcessIpConfig_FlushDNS"/> will be implementing this to return a 
            /// </summary>
            /// M<param name="message">expects message to inform user if failure of platform check</param>
            /// <returns>return true if ok and false if not</returns>
            /// 
            public bool CheckPlatformNeed(out string message);
        }
        /// <summary>
        /// NOT IMPLEMENTED ATM. Implement this and return true to opt into wanting your tool's respond discarded after the last responce.
        /// IMPORTANT! Be sure to spell it out in the <see cref="IButlerToolBaseInterface.ToolDescription"/>
        /// </summary>
        public interface IButlerToolSharpRemoval
        {
            public bool WantSharpRemoval { get; set; }
        }
 
     
    

        /// <summary>
        /// If Implemented, this is called before your first call of the tool (ie after constructor and when you first add it to a butler3)
        /// </summary>
        public interface IButlerToolSpinup
        {
            public void Initialize();
        }


        /// <summary>
        /// If Implemented this is called when prepping for disposal;
        /// </summary>
        public interface IButlerToolWindDown
        {
            public void WindDown();
        }


        /// <summary>
        /// The core tool protocol.  Your class can either subclass <see cref=".ButlerToolBase"/> which uses this  or go direct to this
        /// </summary>
        /// <remarks>All ButlerSDK tools must at minimum implement this</remarks>
        public interface IButlerToolBaseInterface
        {
        /// <summary>
        /// Defines the tool name. Must Be unique in whatever list we send to to the tracker class <see cref="ButlerToolBench"/> and by extention OpenAI llm. There are a few checks in <see cref="Butler"/> and <see cref="ButlerToolBench"/> to prevent invalid names from reaching the OpenAI sdk and bringing ButlerSDK down
        /// </summary>
        /// <remarks>A limit on the <see cref="ButlerToolBench"/> code is it'll reject Tool Names of null or empty</remarks>
        public string ToolName { get; }
            /// <summary>
            /// This is the tool version. Currently is filler but future may be used to difference 
            /// </summary>
            public string ToolVersion { get; }
            /// <summary>
            /// Get a text description of the tool. This is fed to the open ai llm and should be used to instruct the llm how to use the tool.
            /// </summary>
            public string ToolDescription { get; }

        /// <summary>
        /// This routine should be called by your <see cref="ResolveMyTool(ChatToolCall)"/>. It should return false if argument validation failed and true otherwise
        /// </summary>
        /// <param name="Call">the result of single request by <see cref="ChatClient.CompleteChat(IEnumerable{ChatMessage}, ChatCompletionOptions, CancellationToken)"/> <see cref="ChatToolCall"/></param>
        /// <param name="FunctionParse">If you need to JsonDocument parse the argument valid - go for it. Ok to set to null. </param>
        /// <returns></returns>
        /// <remarks>Your routine should use FunctionParse first if not null. If null,  make one yourself if ChatTool.FunctionArguments and JsonDocument"/>
        /// UPDATE:  If FunctionParse is not null - check that.  Otherwise Check Call's arguments. fail if both are null</remarks>
        public bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse);
        //public bool ValidateToolArgs(ButlerChatToolCallMessage? Call);
        /// <summary>
        /// Resolve a tool call.
        /// </summary>
        /// <param name="FunctionCallArguments"></param>
        /// <param name="FuncId"></param>
        /// <param name="Call"></param>
        /// <returns></returns>
        public ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call);
        

        /// <summary>
        /// Get the JSON string the LLM parses to understand this tool
        /// </summary>
        /// <returns></returns>
        public string GetToolJsonString();

        }

    /// <summary>
    /// If your tool is async, implement this instead of <see cref="IButlerToolBaseInterface.ResolveMyTool(string, string, ButlerChatToolCallMessage)"/>
    /// Additionally, YOUR <see cref="IButlerToolBaseInterface.ResolveMyTool(string, string, ButlerChatToolCallMessage?)"/> will NOT BE CALLED
    /// </summary>
    public interface IButlerToolAsyncResolver
    {
        public Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call);
    }

    /// <summary>
    /// A System tool is one that acts on a butler and/or tool kit. It is essentially paired a bit like Magic the Gathering Soul Bond. To put it another way, System tool instances should *not* be shared between butler instances at the same time.
    /// </summary>
    public interface IButlerSystemToolInterface: IButlerToolBaseInterface
    {

        /// <summary>
        /// Pair this system tool with the target butler and ButlerToolBench 
        /// </summary>
        /// <param name="UseMe">the interface to the butler to use</param>
        /// <param name="ToolKit">will be if invoked normally the tool kit to use ButlerToolBench</param>
        /// <remarks>If you get null on either argument skip that</remarks>
        public void Pair(IButlerChatSession? UseMe, object? ToolKit);

        /// <summary>
        /// Unpair the butler
        /// </summary>
        public void UnpairButler();
        /// <summary>
        /// Unpair the toolkit
        /// </summary>
        public void UnpairToolKit();
    }

    /// <summary>
    /// This being added as interface to a tool means the resolver does *not* check limits such as api rate limiter. USE WITH CARE. 
    /// </summary>
    public interface IButlerCritPriorityTool
    {

    }

    /// <summary>
    /// A tool of this type will being called will not end the turn
    /// </summary>
    public interface IButlerToolInPassing
    {

    }

    /// <summary>
    /// When adding this tool to the <see cref="IButlerChatCompletionOptions"/> interface, the system will also add this prompt to the system level prompt for lifetime of tool in the list.
    /// </summary>
    
    public interface IButlerToolPromptInjection
    {
        /// <summary>
        /// If implemented, butler will call this to get prompt injection text to add to the prompt when this tool is present
        /// </summary>
        /// <returns></returns>
        public string GetToolSystemDirectionText();
    }

    /// <summary>
    /// If you use other tools in your tool, protocol is to do this.
    /// </summary>
    public interface IButlerToolContainsPrivateTools
    {
        /// <summary>
        /// Expose the inner collection of tools this tool contains.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IButlerToolBaseInterface> GetInterfaces();
    }

    /// <summary>
    /// If implemented, this adds a temporary directive at system level. 
    /// </summary>
    public interface IButlerToolPostCallInjection
    {
        /// <summary>
        /// If implemented, this is called and put in system prompt post tool call. Does not persist after end of ai turn
        /// </summary>
        /// <returns></returns>
        public string GetToolPostCallDirection();
    }

    /// <summary>
    /// This being on the tool means while fully in the ecosystem, it's *not* sent to the LLM, for example <see cref="IButlerSystemToolInterface"/>
    /// </summary>
    public interface IButlerPassiveTool: IButlerToolPromptInjection
    {

    }






    /// <summary>
    /// The butler interface
    /// </summary>
    public interface IButlerChatSession: IDisposable, IButlerChatSession_ToolQuery, IButlerChatSession_ToolRemoval, IButlerChatSession_UserToolAdd, IButlerChatSession_SystemToolAdd, IButlerChatSession_ToolAutoUpdate
    {

    }

    /// <summary>
    /// Interface for Butler to expose triggering updates on its tool list
    /// </summary>
    public interface IButlerChatSession_ToolAutoUpdate
    {
        /// <summary>
        /// Triger a recalculation on added tools to the provider's tool collection matches butler's
        /// </summary>
        public void RecalcTools();

        /// <summary>
        /// if set, any changes on tool collection *should* trigger <see cref="RecalcTools"/>. Useful to disable that when updating a lot.
        /// </summary>
        public bool AutoUpdateTooList { get; set; }
    }

    /// <summary>
    /// Interface for Butler to expose Searching for tool instances it has that are live and asking if a tool exists
    /// </summary>
    public interface IButlerChatSession_ToolQuery
    {
        /// <summary>
        /// Search for a tool by name, returning interface to it if it exists are null if not.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public IButlerToolBaseInterface? SearchTool(string Name);

        /// <summary>
        /// test if butler has a tool by name x
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool ExistsTool(string Name);
    }
    
    /// <summary>
    /// Inteface for Butler to expose removing tools from its collection
    /// </summary>
    public interface IButlerChatSession_ToolRemoval
    {
        /// <summary>
        /// Delete tool from its collection
        /// </summary>
        /// <param name="name">name of tool</param>
        /// <param name="TriggerCleanup">if set, triggers <see cref="IButlerToolWindDown"/>, <see cref="IDisposable"/> and the unpairing if system tool </param>
        /// <param name="SkipSystemTools">if set, silently discard demoving system tools</param>
        public void DeleteTool(string name, bool TriggerCleanup = true, bool SkipSystemTools = true);
        /// <summary>
        /// Delet all tools in the butler's tool kit
        /// </summary>
        /// <param name="TriggerCleanup">if set, triggers <see cref="IButlerToolWindDown"/>, <see cref="IDisposable"/> and the unpairing if system tool </param>
        /// <param name="SkipSystemTools">if set, silently discard demoving system tools</param>
        public void DeleteAllTools(bool TriggerCleanup = true, bool SkipSystemTools = true);
    }

    /// <summary>
    ///  Inteface to expose adding a new tool to butler's tool kit. Should trigger <see cref="IButlerChatSession_ToolAutoUpdate.RecalcTools"/> if <see cref="IButlerChatSession_ToolAutoUpdate.AutoUpdateTooList "/> is true
    /// </summary>
    public interface IButlerChatSession_UserToolAdd
    {
        /// <summary>
        ///  Add a non system tool to the tool kit. Your implementation should call <see cref="IButlerChatSession_SystemToolAdd.AddSystemTool(IButlerSystemToolInterface)"/> if a system tool gets passed here
        /// </summary>
        /// <param name="tool"></param>
        public void AddTool(IButlerToolBaseInterface tool);
    }

    /// <summary>
    ///  Interface to expose adding a new ststem tool to butler's tool kit. The system tool shouild get automatiicalled paired to this butler instance. This Should trigger <see cref="IButlerChatSession_ToolAutoUpdate.RecalcTools"/> if <see cref="IButlerChatSession_ToolAutoUpdate.AutoUpdateTooList "/> is true
    /// </summary>
    public interface IButlerChatSession_SystemToolAdd
    {
        public void AddSystemTool(IButlerSystemToolInterface systemTool);
    }

    

}
