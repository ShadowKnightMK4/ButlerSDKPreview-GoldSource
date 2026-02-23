using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security;
using ButlerProtocolBase.ToolSecurity;


namespace ButlerSDK.ToolSupport.Bench
{
    

    /// <summary>
    ///  This represents a collection of tools the <see cref="Butler"/> will have usable.
    /// </summary>
    public class ButlerToolBench: IButlerToolBench
    {
        /// <summary>
        /// When a tool does not have the attribute set, treat it as this attribute.
        /// </summary>
        protected ToolSurfaceScope DefaultUnspecified = ToolSurfaceScope.MaxAvailablePermissions;
       
        #region static strings
        const string LimitExceeded = "The tool's limit has been reached. Reset the inventory or try again later.";
        const string EmptyTool = "A tool in the list is actually blank. This is a a software error.";
        const string ToolValidateFailureArg = "The tool's validation code rejected the arguments presented.";
        #endregion
        ApiKeyRateLimiter Limiter = new();
        Dictionary<string, IButlerToolBaseInterface> Tools = new();
        private bool disposedValue;

        /// <summary>
        /// How many tools does this instance have
        /// </summary>
        public int ToolCount => Tools.Count;

        /// <summary>
        /// If enabled, public routines should use lock() to sync access. Why Default? Butler3 uses individual instances of this rather than shared
        /// </summary>
        public bool MultiThreadGuard { get; set; } = false;


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
            ArgumentNullException.ThrowIfNull(Tool, nameof(Tool));
            if (!Regex.IsMatch(Tool.ToolName, ToolNameRegex))
            {
                if (ThrowFailure) { throw new InvalidToolNameException($"{Tool.GetType().Name} does not define a valid tool name. It should match '^[a-zA-Z0-9_-]+$  or in general terms strictly A-z, 0-0 in any combination and the _ symbol. Nothing beyond that."); }
                return false;
            }
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
        /// <exception cref="ArgumentNullException">If the name argument or tool argument is null</exception>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <exception cref="InvalidOperationException">Will be thrown if the tool's name <see cref="IButlerToolBaseInterface.ToolName"/> is null or empty</exception>
        /// <remarks>This is the common code for the public API. The public API respect <see cref="MultiThreadGuard"/>. This DOES NOT</remarks>
        /// <exception cref="InvalidToolNameException">Can be triggered if validation fails. Note even if false, the routine will reject empty or null names</exception>
        /// <exception cref="PlatformPassFailureException"> Can be triggered if tool returns false on platform pass if it implements that interface</exception>
        /// <exception cref="SecurityException">Can be triggered if the tool requests more access than allowed by <see cref="ToolSurfaceFlagChecking.CheckMinRequirements(IButlerToolBaseInterface, ToolSurfaceScope)"/></exception>"
        internal void AddToolCommon(string name, IButlerToolBaseInterface tool, ToolSurfaceScope ScopeFlags, bool ValidateNames=true, bool PreserveLimits=true )
        {
            // first check if it's all valid
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(tool, nameof(tool));
            
            if (ValidateNames)
    
                if(!ValidateToolName(tool, true))
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
            return Tools.ContainsKey(name);
        }


        /// <summary>
        /// fetch the tool if it exists
        /// </summary>
        /// <param name="name">name of the tool</param>
        /// <returns>returns the tool or null if it doesn't exist</returns>
        public IButlerToolBaseInterface? GetTool(string name)
        {
            IButlerToolBaseInterface? ret = null;
            if (Tools.TryGetValue(name, out ret))
            {
                return ret;
            }
            return ret;
        }

        
        

        /// <summary>
        /// Add a tool and assign default limits
        /// </summary>
        /// <param name="name">unique name for tool, grabbing it from the class itself is OK</param>
        /// <param name="tool">tool to add</param>
        /// <exception cref="ArgumentNullException">If the name argument is null</exception>
        /// <exception cref="ToolAlreadyExistsException">Will be thrown if tool with same name exists</exception>
        /// <exception cref="InvalidOperationException">Will be thrown if the tool's name <see cref="IButlerToolBaseInterface.ToolName"/> is null or empty</exception>
        /// <exception cref="InvalidToolNameException">Can trigger if validation fails i.e. <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> returns false. </exception>
        public void AddTool(string name, IButlerToolBaseInterface tool, bool ValidateNames=true, ToolSurfaceScope Scope = ToolSurfaceScope.NoPermissions)
        {
            
            if (MultiThreadGuard)
                lock (this.Tools)
                {
                    AddToolCommon(name, tool, Scope, ValidateNames);
                }
            else
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
            if (MultiThreadGuard)
            {
                UpdateToolCommon(name, tool, true, Scope);
            }
            else
            {
                UpdateToolCommon(name, tool, true, Scope);
            }
        }

        public void UpdateTool(string name, IButlerToolBaseInterface tool)
        {
            UpdateTool(name, tool, ToolSurfaceScope.StandardReading);
        }
        /// <summary>
        /// Add A tool and assign default limits and Surface Scope settings. Lifts tool name from the tool itself
        /// </summary>
        /// <param name="tool"></param>
        /// <param name="ValidateNames"></param>

        public void AddTool(IButlerToolBaseInterface tool, bool ValidateNames=true)
        {
            AddTool(tool, ToolSurfaceScope.StandardReading, ValidateNames);
        }

        /// <summary>
        /// Add A tool and assign default limits. Lifts tool name from the tool itself
        /// </summary>
        /// <param name="tool"></param>
        /// <param name="ValidateNames"></param>
        public void AddTool(IButlerToolBaseInterface tool, ToolSurfaceScope Scope,  bool ValidateNames=true)
        {
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    AddToolCommon(tool.ToolName, tool, Scope, true, false);
                }
            }
            else
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
        /// <remarks>Is a stub to <see cref="AddTool(string, IButlerToolBaseInterface)"/> as the class implements the interface</remarks>
        /// <exception cref="InvalidToolNameException">Can trigger if validation fails i.e. <see cref="ValidateToolName(IButlerToolBaseInterface, bool)"/> returns false. </exception>
        public void AddTool(string name, IButlerToolBaseInterface tool, ToolSurfaceScope Scope, bool ValidateNames)
        {
            
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    AddToolCommon(name, tool as IButlerToolBaseInterface, Scope, ValidateNames);
                }
            }
            else
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
            if (MultiThreadGuard)
            {
                RemoveToolCommon(name, AllowCleanup != true, DoNotRemoveSystemTools);
            }
            else
            {
                lock (Tools)
                {
                    RemoveToolCommon(name, AllowCleanup != true, DoNotRemoveSystemTools);
                }
            }
        }

        public void RemoveAllTools(bool AllowCleanup, bool DoNotRemoveSystemTools)
        {
            var list = Tools.Keys.ToList();
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    foreach (var name in list)
                    {
                        RemoveToolCommon(name, AllowCleanup != true, DoNotRemoveSystemTools);
                    }
                }
            }
            else
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
            Limiter.AssignNewServiceLimit(name, limit);
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
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    return CallToolFunctionInternalSync(null, CallId, Arguments, Tool, out OK);
                }
            }
            else
            {
                return CallToolFunctionInternalSync(null, CallId, Arguments, Tool, out OK);
            }
        }
        public ButlerChatToolResultMessage? CallToolFunction(ButlerChatToolCallMessage msg, out bool OK)
        {
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    return CallToolFunctionInternalSync(msg.ToolName, msg.Id, msg.FunctionArguments,null, out OK);
                }
            }
            else
            {
                return CallToolFunctionInternalSync(msg.ToolName, msg.Id, msg.FunctionArguments, null, out OK);
            }
        }

        public ButlerChatToolResultMessage? CallToolFunction(string FunctionName, string CallId, string Arguments, out bool OK)
        {
            if (MultiThreadGuard)
            {
                lock (Tools)
                {
                    return CallToolFunctionInternalSync(FunctionName, CallId, Arguments, null, out OK);
                }
            }
            else
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
        internal ButlerChatToolResultMessage? CallToolFunctionInternalSync(string? FunctionName, string? CallId, string? Arguments, IButlerToolBaseInterface? ForceUser, out bool OK)
        {
            IButlerToolBaseInterface? Tool = null;
            if (string.IsNullOrEmpty(FunctionName) && (ForceUser is null))
            {
                OK = false;
                throw new ArgumentException("ERROR: FunctionName and ForceUser args must not be null");
            }
            else
            {

                // first check if we got an entry for the function we are calling

                if (FunctionName is not null)
                {
                    Tool = ChatToTool(FunctionName);
                }

                // nope, try subbing the one indicated with ForceUser
                if (Tool is null)
                {
                    if (ForceUser is not null)
                    {
                        Tool = ForceUser;
                        FunctionName = ForceUser.ToolName; // don't forget this, the code below assumes FunctionName is NOT NULL
                    }
                    else
                    {
                        OK = false;
                        throw new ToolNotFoundException("Attempt to call unknown tool");
                    }
                }

                // still nope? Give up
                if (Tool is null)
                {
                    OK = false;
                    throw new ToolNotFoundException("Someone added a blank tool to the tool list.");
                }

                /*
                 * This work flow works:
                 * #1, tool must validate its arguments and reject invalid ones,
                 * #2, if #1 passes, do we have permission to call?
                 * #3 if #2 passes,  update the call inventory (or service) and make the call, returning the result;
                 */
                JsonDocument JsonArgs;
                if (Arguments is not null)
                {
                    JsonArgs= JsonSerializer.SerializeToDocument(Arguments);
                    if (JsonArgs.RootElement.ValueKind == JsonValueKind.String)
                    {
                        // defensive check
                        string? PossibleNullString = JsonArgs.RootElement.GetString();
                        if (PossibleNullString is not null)
                            JsonArgs = JsonDocument.Parse(PossibleNullString);
                        else
                        {
                            // fall back to none.
                            JsonArgs = JsonSerializer.SerializeToDocument("{}");
                        }
                    }
                }
                else
                {
                    // default args. NONE
                    JsonArgs = JsonSerializer.SerializeToDocument("{}");
                }

                if (Tool.ValidateToolArgs(null, JsonArgs))
                {
                    bool HasPermission;
                    if (Tool is IButlerCritPriorityTool) // crit priority tools can be called as much as the LLM or the thing scheduling tools wants. Treat with care.
                        HasPermission = true;
                    else
                    {
                        HasPermission = Limiter.CheckForCallPermission(FunctionName!);
                    }
                    // upper code already establishes the name of the function  is not null
                    if (!HasPermission)
                    {
                        OK = false;
                        return new ButlerChatToolResultMessage(CallId, LimitExceeded);
                    }
                    else
                    {
                        Limiter.ChargeService(FunctionName!, 1);
                        OK = true;
                        return Tool.ResolveMyTool(Arguments, CallId, null);
                    }
                }
                else
                {
                    OK = false;
                    var ret = new ButlerChatToolResultMessage(CallId, ToolValidateFailureArg, Arguments);
                    ret.ToolName = Tool.ToolName;
                    return ret;
                }
            }
        }

        internal async Task<ButlerChatToolResultMessage?> CallToolFunctionInternalAsync(string? FunctionName, string? CallId, string Arguments, IButlerToolBaseInterface? ForceUser)
        {
     
            IButlerToolBaseInterface? Tool = null;
            if (string.IsNullOrEmpty(FunctionName) && (ForceUser is null))
            {
            
                throw new ArgumentException("ERROR: FunctionName and ForceUser args must not be null");
            }
            else
            {

                // first check if we got an entry for the function we are calling

                if (FunctionName is not null)
                {
                    Tool = ChatToTool(FunctionName);
                }

                // nope, try subbing the one indicated with ForceUser
                if (Tool is null)
                {
                    if (ForceUser is not null)
                    {
                        Tool = ForceUser;
                        FunctionName = ForceUser.ToolName; // don't forget this, the code below assumes FunctionName is NOT NULL
                    }
                    else
                    {
                     
                        throw new ToolNotFoundException("Attempt to call unknown tool");
                    }
                }

                // still nope? Give up
                if (Tool is null)
                {
                       throw new ToolNotFoundException("Someone added a blank tool to the tool list.");
                }

                /*
                 * This work flow works:
                 * #1, tool must validate its arguments and reject invalid ones,
                 * #2, if #1 passes, do we have permission to call?
                 * #3 if #2 passes,  update the call inventory (or service) and make the call, returning the result;
                 */
                JsonDocument JsonArgs = JsonSerializer.SerializeToDocument(Arguments);
                if (JsonArgs.RootElement.ValueKind == JsonValueKind.String)
                {
                    string? TempHolding = JsonArgs.RootElement.GetString();
                    if (TempHolding is not null)
                    {
                        JsonArgs = JsonDocument.Parse(TempHolding);
                    }
                    else
                    {
                        JsonArgs = JsonDocument.Parse("{}");
                    }
                }
                if (Tool.ValidateToolArgs(null, JsonArgs))
                {
                    bool HasPermission;
                    if (Tool is IButlerCritPriorityTool) // crit priority tools can be called as much as the LLM or the thing scheduling tools wants. Treat with care.
                        HasPermission = true;
                    else
                    {
                        HasPermission = Limiter.CheckForCallPermission(FunctionName!);
                    }
                    // upper code already establishes the name of the function  is not null
                    if (!HasPermission)
                    {
                         return new ButlerChatToolResultMessage(CallId, LimitExceeded);
                    }
                    else
                    {
                        Limiter.ChargeService(FunctionName!, 1);

                        if (Tool is IButlerToolAsyncResolver AsyncTool)
                        {
                            return await AsyncTool.ResolveMyToolAsync(Arguments, CallId, null);
                        }
                        else
                        {
                            return Tool.ResolveMyTool(Arguments, CallId, null);
                        }
                    }
                }
                else
                {
               
                    var ret = new ButlerChatToolResultMessage(CallId, ToolValidateFailureArg, Arguments);
                    ret.ToolName = Tool.ToolName;
                    return ret;
                }
            }
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
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<ButlerChatToolCallMessage?> CallToolFunctionAsync(IButlerToolBaseInterface targetTool, string CallID, string Arguments)
        {
            var ret = await CallToolFunctionInternalAsync(null, CallID, Arguments, targetTool);
            if (ret is null)
            {
                return null;
            }
            else
            {
                return ret;
            }
        }

        #endregion
    }

}
