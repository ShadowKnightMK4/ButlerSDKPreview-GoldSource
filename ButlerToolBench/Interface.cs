using ButlerToolContract;
using ButlerToolContract.DataTypes;

namespace ButlerSDK.ToolSupport.Bench
{

    /// <summary>
    /// This defines a control for the tool thing to abstract a bit of it away from hard need on <see cref="ButlerToolBench"/>
    /// </summary>
    public interface IButlerToolKitQueryAndGet
    {
        public IButlerToolBaseInterface? GetTool(string name);
        public bool ExistsTool(string name);
        public int ToolCount { get; }
        public IEnumerable<string> ToolNames { get; }
    }


    /// <summary>
    /// Defines the bare minimal tool call routine(s) that <see cref="ToolResolver.RunSchedule(ButlerToolBench)"/> uses, letting anyone swap out what is called with varying complexity.
    /// </summary>
    public interface IButlerToolKitCallableSync
    {
        /// <summary>
        /// Call a tool function. 
        /// </summary>
        /// <param name="FunctionName">Name of the function to call</param>
        /// <param name="CallId">the LLM defined caller ID if any - passed as is</param>
        /// <param name="Arguments">Argument to pass. Should be valid json.</param>
        /// <param name="OK">receive a success or failure flag</param>
        /// <returns>Returns m message to add to a chat log if any of the tool's call result</returns>
        public ButlerChatToolResultMessage? CallToolFunction(string FunctionName, string CallId, string Arguments, out bool OK);
        public ButlerChatToolResultMessage? CallToolFunction(IButlerToolBaseInterface Tool, string CallId, string Arguments, out bool OK);


    }

    

    /// <summary>
    /// Shortant for Async and Sync calling
    /// </summary>
    public interface IButlerToolKitCallable : IButlerToolKitCallableSync, IButlerToolKitAsyncCallable
    {

    }
    /// <summary>
    /// Invoke an async call
    /// </summary>
    public interface IButlerToolKitAsyncCallable
    {
        public Task<ButlerChatToolCallMessage?> CallToolFunctionAsync(IButlerToolBaseInterface targetTool, string CallID, string Arguments);
    }


    public interface IButlerToolKitMutate
    {
        /// <summary>
        /// Add a tool and assign default limits
        /// </summary>
        /// <param name="name">unique name for tool, grabbing it from the class itself is OK</param>
        /// <param name="tool">tool to add</param>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <remarks>Is a stub to <see cref="AddTool(string, IButlerToolBaseInterface)"/> as the class implements the interface</remarks>
        /// <exception cref="InvalidToolNameException">Can trigger if validation fails i.e. <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> returns false. </exception>
        public void AddTool(string name, IButlerToolBaseInterface tool, ToolSurfaceScope Scope, bool ValidateNames);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AllowCleanup"></param>
        /// <param name="DoNotRemoveSystemTools"></param>
        public void RemoveAllTools(bool AllowCleanup, bool DoNotRemoveSystemTools);

        /// <summary>
        /// Remove the tool with this name, calling WindDown and Dispose if they exist in that order if Allowed
        /// </summary>
        /// <param name="name"></param>
        /// <param name="AllowCleanup">If set, calls cleanup routines</param>
        public void RemoveTool(string name, bool AllowCleanup, bool DoNotRemoveSystemTools = true);

    }
    public interface IButlerToolBench : IDisposable, IButlerToolKitQueryAndGet, IButlerToolKitCallable, IButlerToolKitAsyncCallable, IButlerToolKitMutate
    {
    }
}
