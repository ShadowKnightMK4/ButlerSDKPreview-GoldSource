using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security;
using ButlerProtocolBase.ToolSecurity;
using System.Data.SqlTypes;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;


namespace ButlerSDK.ToolSupport.Bench
{
    

    /// <summary>
    ///  This represents a collection of tools the <see cref="Butler"/> will have usable.
    /// </summary>
    public class ButlerToolBench: IButlerToolBench
    {
        ILogger<ButlerToolBench>? Telemetry = null;
        public ButlerToolBench()
        {
            Limiter = new ApiKeyRateLimiter();
            Telemetry = null;
        }

        public ButlerToolBench(IApiKeyRateLimiter CustomLimiter)
        {
            ArgumentNullException.ThrowIfNull(CustomLimiter, nameof(CustomLimiter));    
            Limiter = CustomLimiter;
        }

        public ButlerToolBench(IApiKeyRateLimiter CustomLimiter, ILogger<ButlerToolBench>? TelemetryOutput=default): this(CustomLimiter)
        {
            if (TelemetryOutput is not null)
            {
                Telemetry = TelemetryOutput;
            }
        }
        /// <summary>
        /// When a tool does not have the attribute set, treat it as this attribute.
        /// </summary>
        protected ToolSurfaceScope DefaultUnspecified = ToolSurfaceScope.MaxAvailablePermissions;
       
        #region static strings
        const string LimitExceeded = "The tool's limit has been reached. Reset the inventory or try again later.";
        const string EmptyTool = "A tool in the list is actually blank. This is a a software error.";
        const string ToolValidateFailureArg = "The tool's validation code rejected the arguments presented.";
        #endregion
        IApiKeyRateLimiter Limiter;
        Dictionary<string, IButlerToolBaseInterface> Tools = new();
        private bool disposedValue;

        /// <summary>
        /// How many tools does this instance have
        /// </summary>
        public int ToolCount => Tools.Count;


        private int maxToolThreads = 6;

        [Obsolete("MultiThread Guard is always active in this version. This property is a no-op and will be removed in a future version.")]
        /// <summary>
        /// If enabled, public routines should use lock() to sync access. Why Default? Butler3 uses individual instances of this rather than shared
        /// </summary>
        public bool MultiThreadGuard { get; set; } = true;


        /// <summary>
        /// A way to go thru tool names
        /// </summary>
        public IEnumerable<string> ToolNames => Tools.Keys;

        #region Name validation
        /// <summary>
        /// By default any tools added *must* match this regex.  The default is set to kick out tool names not matching OpenAI's protocol
        /// </summary>
        public static string ToolNameRegex => DefaultToolNameRegEx;
        /// <summary>
        ///  And if for reason needed the default back. Here it is
        /// </summary>
        public const string DefaultToolNameRegEx = "^[a-zA-Z0-9_-]+$";

        /// <summary>
        /// This routine will verify if the name of the tool i.e. <see cref="IButlerToolBaseInterface.ToolName"/> passes the regex contained in <see cref="ToolNameRegex"/>
        /// </summary>
        /// <param name="Tool">tool to check</param>
        /// <param name="ThrowFailure">Want exception on failure</param>
        /// <returns>returns true if success and false if nope</returns>
        /// <exception cref="InvalidToolNameException">triggers if ThrowFailure true and the thing  fails the validate</exception>
        /// <exception cref="ArgumentNullException">If you pass null in tool. This happens</exception>
        /// <remarks>While this routine exposes a way to ensure you don't give your LLM a bad tool name. The <see cref="AddTool(string, ButlerToolBase, bool)"/> API will reject null or empty names as a sanity check regardless</remarks>
        public virtual bool ValidateToolName(IButlerToolBaseInterface Tool, bool ThrowFailure=true)
        {
            if (Tool is null)
            {
                Telemetry?.LogWarning("Attempt to validate a tool name of a null reference. Warning");
            }
            
            
            ArgumentNullException.ThrowIfNull(Tool, nameof(Tool));

            

            if (!Regex.IsMatch(Tool.ToolName, ToolNameRegex))
            {
                var err_msg = "Tool {ToolName} does not define a valid tool name. It should match RegEx '^[a-zA-Z0-9_-]+$  or in general terms strictly A-z, 0-0 in any combination and the _ symbol. Nothing beyond that.";
                Telemetry?.LogWarning(err_msg, Tool.ToolName);
                if (ThrowFailure) { throw new InvalidToolNameException(err_msg.Replace("{ToolName}", Tool.ToolName)); }
                return false;
            }
            Telemetry?.LogTrace("Validating the tool name {name} passed!", Tool.ToolName);
            return true;
        }
        #endregion
        #region common API filters - 
        //  this  region is common code for the public exposed AOI 


        /// <summary>
        /// Add a tool and assign default limits. This is not really intended for general use. Try the public facing first
        /// </summary>
        /// <param name="name">unique name for tool, grabbing it yourself from the class  <see cref="IButlerToolBaseInterface.ToolName"/> is OK </param>
        /// <param name="tool">tool to add. An exception will be thrown if null</param>
        /// <param name="ValidateNames">if set, the <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> routine will be called and the correct exception thrown if validation failed</param>
        /// <param name="PreserveLimits">Mainly for <see cref="UpdateTool(string, IButlerToolBaseInterface)"/> This let's that routine swap the call out while preserving <see cref="Limiter"/> stats</param>
        /// <exception cref="ArgumentNullException">If the name argument or tool argument is null *new* - or if the tool instance passed is null</exception>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <exception cref="InvalidOperationException">Will be thrown if the tool's name <see cref="IButlerToolBaseInterface.ToolName"/> is null or empty</exception>
        /// <remarks>This is the common code for the public API. The public API respect <see cref="MultiThreadGuard"/>. This DOES NOT</remarks>
        /// <exception cref="InvalidToolNameException">Can be triggered if validation fails. Note even if false, the routine will reject empty or null names</exception>
        /// <exception cref="PlatformPassFailureException"> Can be triggered if tool returns false on platform pass if it implements that interface</exception>
        /// <exception cref="SecurityException">Can be triggered if the tool requests more access than allowed by <see cref="ToolSurfaceFlagChecking.CheckMinRequirements(IButlerToolBaseInterface, ToolSurfaceScope)"/></exception>"
        internal void AddToolCommon(string name, IButlerToolBaseInterface tool, ToolSurfaceScope ScopeFlags, bool ValidateNames=true, bool PreserveLimits=true )
        {

            IDisposable? tscope =null;
            // first check if it's all valid

            try
            {
                if (Telemetry is not null)
                {
                    string safe_name;
                    string safe_tool_name;
                    string safe_version;
                    string? safe_enum;

                    if (string.IsNullOrEmpty(name))
                    {
                        safe_name = "Un-named tool";
                    }
                    else
                    {
                        safe_name = name;
                    }

                    if (tool is not null)
                    {
                        if (string.IsNullOrEmpty(tool.ToolName))
                        {
                            safe_tool_name = "No Set tool name in instance.";
                        }
                        else
                        {
                            safe_tool_name = tool.ToolName;
                        }

                        if (string.IsNullOrEmpty(tool.ToolVersion))
                        {
                            safe_version = "Unknown Versioning info";
                        }
                        else
                        {
                            safe_version = tool.ToolVersion;
                        }
                    }
                    else
                    {
                        safe_tool_name = "ERROR: NULL TOOL";
                        safe_version = "ERROR: NULL TOOL";
                    }

                    safe_enum = Enum.GetName(ScopeFlags);

                    if (safe_enum is null)
                    {
                        safe_enum = "UNKNOWN TOOL SURFACE OR INVALID ONE";
                    }
                    IDisposable? tele =

                   tscope =  Telemetry?.BeginScope("Starting attempt to add tool: {name} using tool instance {tinstance} version {verison} using SurfaceScope of {Scope}",
                        safe_name,
                        safe_tool_name,
                        safe_version,
                        safe_enum);
                }

                ArgumentNullException.ThrowIfNull(name, nameof(name));
                ArgumentNullException.ThrowIfNull(tool, nameof(tool));
                if (tool is null)
                {
                    Telemetry?.LogCritical("Attempt to add a tool instance under name {name} that's actually null!", name);
                    throw new ArgumentNullException("Tool instance passed is null.");
                }

                if (ValidateNames)

                    if (!ValidateToolName(tool, true))
                    {
                        // strictly speaking, this will never be hit, assuming validate tool name throws exception (as it should) on failure
                        throw new InvalidToolNameException("Validation failed for a tool name");
                    }

                if (string.IsNullOrEmpty(tool.ToolName))
                {
                    throw new InvalidOperationException($"The name of the tool passed in with {nameof(tool)} is actually null or an empty. That doesn't work with the protocol");
                }
                // does the tool exist?
                if (ExistsTool(name))
                    throw new ToolAlreadyExistsException(name);
                else
                {
                    // first check if tool opted into a platform check
                    if (tool is IButlerToolPlatformPass PlatformChecker)
                    {
                        string msg = string.Empty;


                        if (PlatformChecker.CheckPlatformNeed(out msg) == false)
                        {
                            // tool rejected platform check(returned false) throw exception
                            throw PlatformPassFailureException.DefaultBuilder(msg, tool.ToolName);
                        }
                    }

                    if (ToolSurfaceFlagChecking.HasToolSurfaceFlags(tool))
                    {
                        if (ToolSurfaceFlagChecking.CheckMinRequirements(tool, ScopeFlags))
                        {
                            // add it with the default limits and call Initialize() if defined
                            Tools.Add(name, tool);
                        }
                        else
                        {

                            throw new SecurityException($"Tool {tool.ToolName} attempt to add but requests more access than allowed. Rejecting it.");

                        }
                    }
                    else
                    {
                        // no flags treat as max permission
                        if (ScopeFlags != ToolSurfaceScope.MaxAvailablePermissions)
                        {
                            throw new SecurityException($"Tool {tool.ToolName} has no attributes set. ScopeSurface passed not max requested. Rejecting it");
                        }
                        else
                        {
                            Tools.Add(name, tool);
                        }
                    }


                    // pretty much atm UpdateTool() uses this. If set, PreserveLimits means while we
                    // be swapping this class object out, the service limit class tracking thing
                    // is not changed.
                    if (tool is not IButlerPassiveTool)
                    {
                        if (PreserveLimits)
                        {
                            if (Limiter.DoesServiceExist(name) == false)
                            {
                                Limiter.AddService(name, 0, 200, 200, ButlerApiLimitType.PerCall);
                            }
                        }
                        else
                        {
                            Limiter.RemoveService(name);
                            Limiter.AddService(name, 0, 200, 200, ButlerApiLimitType.PerCall);
                        }
                        if (tool is IButlerToolSpinup spin)
                            spin.Initialize();
                    }
                }
            }
            finally
            {
                if (tscope is not null) tscope.Dispose();
            }
        }

        /// <summary>
        /// remove the tool with this name if it exists and call the <see cref="IButlerToolWindDown"/>  if it exists.
        /// </summary>
        /// <param name="name">tool to remove</param>
        /// <param name="SkipCleanup">If true disables cleanup- generally don't want this true but if you're swapping tools between butler3 stuff (why), disable it</param>
        /// <remarks>If the tool is not found, dos nothing</remarks>
        internal void RemoveToolCommon(string name, bool SkipCleanup=false, bool DoNotRemoveSystemTools = true)
        {
            IButlerToolBaseInterface? tool = null;
            if (!Tools.TryGetValue(name, out tool))
            {
                return;
            }
            else
            {
                
                IButlerSystemToolInterface? SysTool = tool as IButlerSystemToolInterface;
                if (SysTool is not null)
                {
                    if (DoNotRemoveSystemTools)
                    {
                        return;
                    }
                }
                if (tool is not null)
                {
                    if (!SkipCleanup)
                    {
                        // check for Winding Down interface and dispose, calling them in order
                        if (tool is IButlerToolWindDown wind)
                        {
                            wind.WindDown();
                        }

                        // not part of the expected standard, but I feel good practice
                        if (tool is IDisposable dis)
                        {
                            dis.Dispose();
                        }

                    }

                    // system tool is a special case: added pairs the tool to the requested butler (think MTG soul bond).
                    // if this tool is removed from its paired butler, we unpair them even if clean up is not skipped.
                    // result?:
                    // Butler.AddSystemTool(x) can be brainlessly removed via Butler.RemoveTool()
                        SysTool?.UnpairButler();
                        SysTool?.UnpairToolKit();
                    
                }
            }
            // finally remove it from our collection
            Tools.Remove(name);
        }

        #endregion

        #region tool adding and removing and finding
        /// <summary>
        /// Return if we have a tool by that name
        /// </summary>
        /// <param name="name">check for this</param>
        /// <returns></returns>
        public bool ExistsTool(string name)
        {
            lock (Tools)
            {
                
                bool ret = Tools.ContainsKey(name);
                Telemetry?.LogTrace("Tested Tool bench list for existence of {name}.  Status Existing: {ret}", name, ret);
                return ret;
            }
        }


        /// <summary>
        /// fetch the tool if it exists
        /// </summary>
        /// <param name="name">name of the tool</param>
        /// <returns>returns the tool or null if it doesn't exist</returns>
        public IButlerToolBaseInterface? GetTool(string name)
        {
            lock (Tools)
            {
                IButlerToolBaseInterface? ret = null;
                if (Tools.TryGetValue(name, out ret))
                {
                    if (ret is not null)
                    {
                        Telemetry?.LogTrace("Tool named {name} found. Instance retrieved is not null. Tool name: {name} Tool Version {version}", name, ret.ToolName, ret.ToolVersion); 
                    }
                    else
                    {
                        Telemetry?.LogWarning("Tool name {name} found. Warning retrieved instance is null. This will lead to triggering exceptions. Don't add a null instance.", name);
                    }
                }
                else
                {
                    Telemetry?.LogTrace("Tool name {name} attempt to find.  FAILED. Does not exist", name);
                }
                return ret;
            }
        }

        
        

        /// <summary>
        /// Add a tool and assign default limits
        /// </summary>
        /// <param name="name">unique name for tool, grabbing it from the class itself is OK</param>
        /// <param name="tool">tool to add</param>
        /// <exception cref="ArgumentNullException">If the name argument is null OR THE TOOL instance is null</exception>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <exception cref="InvalidOperationException">Will be thrown if the tool's name <see cref="IButlerToolBaseInterface.ToolName"/> is null or empty</exception>
        /// <exception cref="InvalidToolNameException">Can trigger if validation fails i.e. <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> returns false. </exception>
        public void AddTool(string name, IButlerToolBaseInterface tool, bool ValidateNames=true, ToolSurfaceScope Scope = ToolSurfaceScope.NoPermissions)
        {

            lock (this.Tools)
            {
                AddToolCommon(name, tool, Scope, ValidateNames);
            }
        }


        


        /// <summary>
        /// Remove the tool if it exists, and swap with the current one
        /// </summary>
        /// <param name="name">name of the tool</param>
        /// <param name="tool">new instance to replace it with</param>
        internal void UpdateToolCommon(string  name, IButlerToolBaseInterface tool, bool PreserveLimits, ToolSurfaceScope AccessFlag)
        {
            if (ToolSurfaceFlagChecking.CheckMinRequirements(tool, AccessFlag) == false)
            {
                throw new SecurityException($"Attempt to update a tool {name} with a tool that requests more access than allowed. Rejecting it.");
            }
            RemoveToolCommon(name);
            AddToolCommon(name, tool, AccessFlag);
        }

        public void UpdateTool(string name, IButlerToolBaseInterface tool, ToolSurfaceScope Scope)
        {
            lock (Tools)
            {
                UpdateToolCommon(name, tool, true, Scope);
            }
        }

        public void UpdateTool(string name, IButlerToolBaseInterface tool)
        {
            /* NOTE DO NOT LOCK THIS AS IT FOWARDS TO APUBLIC API THAT TRY LOCKING */
            UpdateTool(name, tool, ToolSurfaceScope.StandardReading);
        }
        /// <summary>
        /// Add A tool and assign default limits and Surface Scope settings. Lifts tool name from the tool itself
        /// </summary>
        /// <param name="tool">tool to add</param>
        /// <param name="ValidateNames"></param>
        /// <exception cref="ArgumentNullException">This till trigger if the tool passed is null</exception>

        public void AddTool(IButlerToolBaseInterface tool, bool ValidateNames=true)
        {
            /* NOTE DO NOT LOCK THIS AS IT FOWARDS TO APUBLIC API THAT TRY LOCKING */
            AddTool(tool, ToolSurfaceScope.StandardReading, ValidateNames);
        }

        /// <summary>
        /// Add A tool and assign default limits. Lifts tool name from the tool itself
        /// </summary>
        /// <param name="tool">tool to add. Must not be null</param>
        /// <param name="ValidateNames">Enforce the default name validation of the tool (before the underling provider / LLM gets it.)</param>
        /// <exception cref="ArgumentNullException">This will trigger if the tool instance passed is null </exception>
        public void AddTool(IButlerToolBaseInterface tool, ToolSurfaceScope Scope,  bool ValidateNames=true)
        {
            lock (Tools)
            {
                AddToolCommon(tool.ToolName, tool, Scope, true, false);
            }
        }
        
        /// <summary>
        /// Add a tool and assign default limits
        /// </summary>
        /// <param name="name">unique name for tool, grabbing it from the class itself is OK</param>
        /// <param name="tool">tool to add</param>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <exception cref="ArgumentNullException">This will be triggered if the tool instance passed is null</exception>
        /// <remarks>Is a stub to <see cref="AddTool(string, IButlerToolBaseInterface)"/> as the class implements the interface</remarks>
        /// <exception cref="InvalidToolNameException">Can trigger if validation fails i.e. <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> returns false. </exception>
        public void AddTool(string name, IButlerToolBaseInterface tool, ToolSurfaceScope Scope, bool ValidateNames)
        {

            lock (Tools)
            {
                AddToolCommon(name, tool as IButlerToolBaseInterface, Scope, ValidateNames);
            }

        }

        /// <summary>
        /// Remove the tool with this name, calling WindDown and Dispose if they exist in that order if Allowed
        /// </summary>
        /// <param name="name"></param>
        /// <param name="AllowCleanup">If set, calls cleanup routines</param>
        public void RemoveTool(string name, bool AllowCleanup, bool DoNotRemoveSystemTools=true)
        {
            lock (Tools)
            {
                RemoveToolCommon(name, AllowCleanup != true, DoNotRemoveSystemTools);
            }
        }

        public void RemoveAllTools(bool AllowCleanup, bool DoNotRemoveSystemTools)
        {
            var list = Tools.Keys.ToList();
            lock (Tools)
            {
                foreach (var name in list)
                {
                    RemoveToolCommon(name, AllowCleanup != true, DoNotRemoveSystemTools);
                }
            }
        }
        #endregion

        #region adjusting tool limits
        public void UpdateInventoryLimit(string name, ulong limit)
        {
            lock (Tools)
            {
                Telemetry?.LogInformation("Assigning new limit to tool {name} of {limit} calls.", name, limit);
                Limiter.AssignNewServiceLimit(name, limit);
            }
        }

        #endregion

        #region tool calling and resolving

        
  

        /// <summary>
        /// return a tool instance based on its name from us
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IButlerToolBaseInterface? ChatToTool(string name)
        {
            IButlerToolBaseInterface? ret = null;
            try
            {
                Telemetry?.LogInformation("Resolved tool name {name} to instance of type {Type} version {toolversion} with name {Name}", name, ret.GetType().FullName, ret.ToolVersion, ret.ToolName);
                ret = Tools[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            return ret;
        }


        public ButlerChatToolResultMessage? CallToolFunction(IButlerToolBaseInterface Tool, string CallId, string Arguments, out bool OK)
        {
            lock (Tools)
            {
                return CallToolFunctionInternalSync(null, CallId, Arguments, Tool, out OK);
            }
        }
        public ButlerChatToolResultMessage? CallToolFunction(ButlerChatToolCallMessage msg, out bool OK)
        {
            lock (Tools)
            {
                return CallToolFunctionInternalSync(msg.ToolName, msg.Id, msg.FunctionArguments, null, out OK);
            }
        }

        public ButlerChatToolResultMessage? CallToolFunction(string FunctionName, string CallId, string Arguments, out bool OK)
        {
            lock (Tools)
            {
                return CallToolFunctionInternalSync(FunctionName, CallId, Arguments, null, out OK);
            }

        }

        /// <summary>
        /// Common point of the Other CallToolFunctions 
        /// </summary>
        /// <param name="FunctionName">Optional: if null, ForceUser must not be null and be a valid tool to invoke and return </param>
        /// <param name="CallId">Typically provided by OpenAi/LLM BUT I use "TESTFUNC" in Unit Testing. The value is unchanged by this function</param>
        /// <param name="Arguments">Arguments to pass to the tool- JSON. If you pass null, this routine subs <see cref="JsonSerializer.SerializeToDocument()"/> with a '{}' argument. Note this routine SHOULD NOT BE TYPICALLY CALLED NULL. The public routines DO NOT DO THAT. </param>
        /// <param name="ForceUser">if function name be null, this MUST be not null.</param>
        /// <param name="OK">set to true on call work and false on error</param>
        /// <returns>depending on the tool either a <see cref="ButlerChatToolCallMessage"/> or null</returns>
        /// <exception cref="ToolNotFoundException">Is thrown if the name is not in our list</exception>
        /// <remarks>The multo thread guard stuff is in the public api. This internal routine knows no such perks</remarks>
        internal ButlerChatToolResultMessage? CallToolFunctionInternalSync(string? FunctionName, string? CallId, string? Arguments, IButlerToolBaseInterface? ForceUser, out bool OK)
        {
            ButlerChatToolResultMessage? err_reply = null;
            IButlerToolBaseInterface? Tool = null;
            IDisposable? Tele = Telemetry?.BeginScope("Beginning a tool call (call id: {CallID}) for tool {FunctionName} in legacy sync path.", CallId, FunctionName);
            try
            {
                if (CallToolFunctionInternalPREPWORK(FunctionName, CallId, Arguments, ForceUser, ref err_reply, ref Tool))
                {
                    // the tole payed, go run it.

                    if (Tool is IButlerToolAsyncResolver AsyncTool)
                    {
                        Telemetry?.LogTrace("Tool {FunctionName} for call {CallID} is async path. triggering it via offloaded task.", FunctionName, CallId);
                        OK = true;
                        // THE ESCAPE HATCH: 
                        // We push the async execution to the ThreadPool to escape any UI SynchronizationContext,
                        // preventing deadlocks in MAUI/WPF apps, then safely block and unwrap the result.
                        return Task.Run(async () => await AsyncTool.ResolveMyToolAsync(Arguments, CallId, null))
                                   .ConfigureAwait(false)
                                   .GetAwaiter()
                                   .GetResult();
                    }
                    else
                    {
                        Telemetry?.LogTrace("Tool {FunctionName} for call {CallID} is sync legacy path. triggering directly.", FunctionName, CallId);
                        OK = true;
                        // the prepwork should return false, triggering this to never actually work.
                        return Tool!.ResolveMyTool(Arguments, CallId, null);
                    }
                }

                OK = false;
                return err_reply;
            }
            finally
            {
                if (Tele is not null) Tele?.Dispose();
            }
          
        }

        internal bool CallToolFunctionInternalPREPWORK(string? FunctionName, string? CallId, string? Arguments, IButlerToolBaseInterface? ForceUser, ref ButlerChatToolResultMessage? ErrorCode, ref IButlerToolBaseInterface? ToolRef)
        {
            var scope =   Telemetry?.BeginScope("Preparing to call {FunctionName} in {CallID} starting", FunctionName, CallId);
            try
            {
                IButlerToolBaseInterface? Tool = null;
                if (string.IsNullOrEmpty(FunctionName) && (ForceUser is null))
                {
                    var ex = new ArgumentException("ERROR: FunctionName and ForceUser args must not be null");
                    Telemetry?.LogCritical("Fatal Error {msg}", ex.Message);
                    throw ex;
                }
                else
                {
                    ErrorCode = null;
                    // first check if we got an entry for the function we are calling
                    Telemetry?.LogTrace("Trying to Resolve Tool name first");
                    if (FunctionName is not null)
                    {
                        Tool = ChatToTool(FunctionName);
                    }

                    // nope, try subbing the one indicated with ForceUser
                    if (Tool is null)
                    {
                        Telemetry?.LogWarning("First try failed - Name was null");
                        if (ForceUser is not null)
                        {
                            Telemetry?.LogWarning("Using specific passed tool instance instead - tool {ForceTool} version {version} instead", ForceUser.ToolName, ForceUser.ToolVersion);
                            Tool = ForceUser;
                            FunctionName = ForceUser.ToolName; // don't forget this, the code below assumes FunctionName is NOT NULL
                        }
                        else
                        {
                            var ex = new ToolNotFoundException("Attempt to call unknown tool");
                            Telemetry?.LogCritical("Unable to finish this call - no known tool to answer it");
                            throw ex;
                        }
                    }
                    ToolRef = Tool;
                    // still nope? Give up
                    if (Tool is null)
                    {
                        var ex = new ToolNotFoundException("Someone added a blank tool to the tool list.");
                        Telemetry?.LogCritical("Unable to finish this call, there's a tool entry but the object instance is null!");
                        throw ex;
                    }
                    else
                    {
                        Telemetry?.LogTrace("Tool call bound to object {tool}", ToolRef);
                    }

                    /*
                     * This work flow works:
                     * #1, tool must validate its arguments and reject invalid ones,
                     * #2, if #1 passes, do we have permission to call?
                     * #3 if #2 passes,  update the call inventory (or service) and make the call, returning the result;
                     */
                    JsonDocument? JsonArgs = null;
                    JsonDocument? shimdoc = null;
                    try
                    {
                        Telemetry?.BeginScope("Beginning Tool self validation in call {calliD} for tool {FunctionName}", CallId, FunctionName);
                        JsonArgs = JsonSerializer.SerializeToDocument(Arguments);
                        if (JsonArgs.RootElement.ValueKind == JsonValueKind.String)
                        {

                            string? TempHolding = JsonArgs.RootElement.GetString();
                            if (TempHolding is not null)
                            {
                                shimdoc = JsonDocument.Parse(TempHolding);
                            }
                            else
                            {
                                shimdoc = JsonDocument.Parse("{}");
                            }

                            JsonArgs?.Dispose(); // gonna be not null here. Just on the paranoid chance it is, don't take the thing down
                            JsonArgs = shimdoc;
                            shimdoc = null;
                        }

                        if (Tool.ValidateToolArgs(null, JsonArgs))
                        {
                            Telemetry?.LogTrace("Tool {tool} in call {callId} was validated!", ToolRef, CallId);
                            bool HasPermission = false;

                            if (Tool is IButlerCritPriorityTool)
                            {
                                Telemetry?.LogWarning("Warning this is a Crit tool type. No limits imposed. Ensure it does *not* have IButlerCritPriorityTool interface to let it have limits");
                                HasPermission = true;
                            }
                            if (Limiter is IApiKeyRateLimiterAtomicCharge atomicCharge)
                            {
                                Telemetry?.LogTrace("Limiter is atomic. Using that check instead");
                                if (!HasPermission) // crit priority tool check sets to true, triggering skip
                                {
                                    HasPermission = atomicCharge.CheckForCallPermissionAndCharge(FunctionName!);
                                }
                                // upper code already establishes the name of the function  is not null
                                if (!HasPermission)
                                {
                                    Telemetry?.LogError("CalliD  {d} call attempt to exceed limit! Refusing to run.", CallId);
                                    ErrorCode = new ButlerChatToolResultMessage(CallId, LimitExceeded);
                                    return false;
                                }
                                else
                                {
                                    // the tole payed, go run it.
                                    /*
                                    if (Tool is IButlerToolAsyncResolver AsyncTool)
                                    {
                                        return await AsyncTool.ResolveMyToolAsync(Arguments, CallId, null);
                                    }
                                    else
                                    {
                                        return Tool.ResolveMyTool(Arguments, CallId, null);
                                    }*/
                                    Telemetry?.LogTrace("SUCESSFULL CALL of tool {tool} via call {c}", ToolRef, CallId);
                                    return true;
                                }
                            }
                            else
                            {
                                Telemetry?.LogTrace("Limiter is non-atomic. Using that check instead. Warning in heavy threaded env this may let tools be charged *beyond* the limit"); ;

                                // legacy path. It's fine.
                                if (Tool is IButlerCritPriorityTool) // crit priority tools can be called as much as the LLM or the thing scheduling tools wants. Treat with care.
                                {
                                    HasPermission = true;
                                    Telemetry?.LogWarning("Warning this is a Crit tool type. No limits imposed. Ensure it does *not* have IButlerCritPriorityTool interface to let it have limits");
                                }
                                else
                                {
                                    HasPermission = Limiter.CheckForCallPermission(FunctionName!);
                                }
                                // upper code already establishes the name of the function  is not null
                                if (!HasPermission)
                                {
                                    Telemetry?.LogError("CalliD  {d} call attempt to exceed limit! Refusing to run.", CallId);
                                    ErrorCode = new ButlerChatToolResultMessage(CallId, LimitExceeded);
                                    return false;
                                }
                                else
                                {
                                    Telemetry?.LogTrace("Permission check passed! Issuing charge now (legacy).");
                                    Limiter.ChargeService(FunctionName!, 1);

                                    // the tole payed, go run it.
                                    /*
                                    if (Tool is IButlerToolAsyncResolver AsyncTool)
                                    {
                                        return await AsyncTool.ResolveMyToolAsync(Arguments, CallId, null);
                                    }
                                    else
                                    {
                                        return Tool.ResolveMyTool(Arguments, CallId, null);
                                    }*/
                                    return true;
                                }
                            }


                        }
                        else
                        {
                            Telemetry?.LogError("{CallID} tool call self validate reported failure (tool rejected the function parameters!)", CallId);
                            var ret = new ButlerChatToolResultMessage(CallId, ToolValidateFailureArg, Arguments);
                            ret.ToolName = Tool.ToolName;
                            ErrorCode = ret;

                            // load bearing assignment. Either ensure the constructor we use actually sets a message OR we assign.
                            // Dear future reader *musical number* don't remove this this until ensuring in abstractions, the code assigns a result message!
                            ret.Message = ToolValidateFailureArg;
                            return false;
                        }
                    }
                    finally
                    {

                        if (JsonArgs != null) JsonArgs.Dispose();
                        if (shimdoc != null) shimdoc.Dispose();
                    }
                }
            }
            finally
            {
                if (scope is not null)
                {
                    scope?.Dispose();
                }
            }
        }
        internal async Task<ButlerChatToolResultMessage?> CallToolFunctionInternalAsync(string? FunctionName, string? CallId, string Arguments, IButlerToolBaseInterface? ForceUser)
        {

            IDisposable? Tele = Telemetry?.BeginScope("Beginning a tool call (call id: {CallID}) for tool {FunctionName} in async path.", CallId, FunctionName);
            ButlerChatToolResultMessage? ret = null;
            try
            {
                IButlerToolBaseInterface? Tool = null;
                Telemetry?.LogTrace("Starting Prep work for the tool call. {CallId} of tool {FunctionName}", CallId, FunctionName);
                if (CallToolFunctionInternalPREPWORK(FunctionName, CallId, Arguments, ForceUser, ref ret, ref Tool))
                {
                    // the tole payed, go run it.

                    if (Tool is IButlerToolAsyncResolver AsyncTool)
                    {
                        Telemetry?.LogTrace("Tool {FunctionName} is an async tool, running that path way", FunctionName);
                        ret = await AsyncTool.ResolveMyToolAsync(Arguments, CallId, null);
                        if (ret is null)
                        {
                            Telemetry?.LogWarning("Warning: Tool call returned null. It is recommended to not do that");
                        }
                        else
                        {
                            if (ret.Message is not null)
                            {
                                Telemetry?.LogTrace("Results of call {ret}", ret?.Message);
                            }
                            else
                            {
                                Telemetry?.LogWarning("Warning: Message returned is null contents. A provider might throw an error on converting to its LLM");
                            }
                        }
      
                    }
                    else
                    {
                        Telemetry?.LogTrace("Tool {FunctionName} is an sync (legacy) tool, running that path way", FunctionName);
                        // the prep work should return false, triggering this to never actually work.
                        ret = Tool!.ResolveMyTool(Arguments, CallId, null);
                        if (ret is null)
                        {
                            Telemetry?.LogWarning("Warning: Tool call returned null. It is recommended to not do that");
                        }
                        else
                        {
                            if (ret.Message is not null)
                            {
                                Telemetry?.LogTrace("Results of call {ret}", ret?.Message);
                            }
                            else
                            {
                                Telemetry?.LogWarning("Warning: Message returned is null contents. A provider might throw an error on converting to its LLM");
                            }
                        }

                    }
                }
                else
                {
                    Telemetry?.LogWarning("Tool call prep work failure.");
                }
                Telemetry?.LogTrace("Tool Call is finished");
            }
            finally
            {
                if (Tele is not null) Tele?.Dispose();
            }
            
            return ret;



        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (string s in Tools.Keys)
                    {
                        if (Tools[s] is IButlerToolWindDown wind)
                        {
                            wind.WindDown();
                        }
                        if (Tools[s] is IDisposable recycle)
                        {
                            recycle.Dispose();
                        }
                    }
                    Tools.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ButlerToolBench()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Telemetry?.LogTrace("CLEANUP: {ButlerToolBench}", this);
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<ButlerChatToolCallMessage?> CallToolFunctionAsync(IButlerToolBaseInterface targetTool, string CallID, string Arguments)
        {
            IDisposable? scope = 
                Telemetry?.BeginScope("Calling {targetTool} tool version \"{version}\"with {CallID}", targetTool.ToolName, targetTool.ToolVersion, CallID);
            try
            {
                var ret = await CallToolFunctionInternalAsync(null, CallID, Arguments, targetTool);
                Telemetry?.LogTrace("Call {CallID} for {targetTool} tool is finished.", CallID, targetTool);
                if (ret is null)
                {
                    return null;
                }
                else
                {
                    return ret;
                }
            }
            finally
            {
                if (scope is not null)
                    scope.Dispose();
            }
        }

        #endregion
    }

}
