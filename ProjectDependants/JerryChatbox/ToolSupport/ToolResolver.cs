using ButlerLLMProviderPlatform.DataTypes;
using ButlerToolContract.DataTypes;
using ButlerSDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json;
using ButlerToolContract;
using System.Net.NetworkInformation;
using ButlerSDK.ToolSupport.Bench;
using ButlerSDK.Core;


    namespace ButlerSDK.ToolSupport
{

    /// <summary>
    /// Optional Telemetry tool to get info on execution ran
    /// </summary>
    public class ToolResolverTelemetryStats
    {
        /// <summary>
        /// How many tools were run in this pass of type <see cref="IButlerToolInPassing"/>
        /// </summary>
        public uint InPassingCount = 0;
        /// <summary>
        /// How many tools of type <see cref="IButlerCritPriorityTool"/>
        /// </summary>
        public uint CritPriorityCount = 0;
        /// <summary>
        /// use call id for the key.  Blank list means no exceptions caught. Otherwise its a list of exceptions seen while running the tool in a task
        /// </summary>
        public Dictionary<string, List<Exception>> ExceptionsCaught = new();

        public IReadOnlyList<(string CallID, IButlerToolBaseInterface Tool)>? ToolsUsed;

    }
    /*
     * The plan of the schedule is, any tools 
     */
    public class ToolResolver
    {
        /// <summary>
        /// This exception triggers by <see cref="ToolResolver"/> attempt to run schedule with no tools to run
        /// </summary>
        public class NoToolScheduledException : Exception
        {
            public NoToolScheduledException():base()
            {

            }
            public NoToolScheduledException(string? message) : base(message)
            {

            }

            public NoToolScheduledException(string? message, Exception? innerException) : base(message, innerException)
            {
            }
        }

        internal class ToolTimeSlot
        {
            public int StreamingIndex = 0;
            /// <summary>
            /// either <see cref="ChatToolCall.Id"/> or <see cref="StreamingChatToolCallUpdate.Id"/>
            /// </summary>
            public StringBuilder ID = new();
            /// <summary>
            /// either <see cref="ChatToolCall.FunctionArguments"/> or <see cref="StreamingChatToolCallUpdate.FunctionArgumentsUpdate"/>
            /// </summary>
            public StringBuilder ToolArgumentsPart = new();
            /// <summary>
            /// either <see cref="ChatToolCall.FunctionName"/> or <see cref="StreamingChatToolCallUpdate.FunctionName"/>
            /// </summary>
            public StringBuilder ToolName = new();
            /// <summary>
            /// Collection of any exceptions triggered by the tool while it ran.
            /// </summary>
            public List<Exception> Failures = new();
            public Thread? Self=null; // if we're running different thread ie spawned diff thread, this is us
            public ButlerChatToolCallMessage? Results;
            /// <summary>
            /// see <see cref="ButlerToolContract.IButlerToolInPassing"/>. If set <see cref="ButlerPostProcessing.StreamResponseAsync(Butler.ChatMessageStreamHandler, ButlerSDK.ButlerPostProcessing.IButlerPostProcessorHandler?, bool, int, CancellationToken)"/> will not end the ai turn on this tool call
            /// </summary>
            public bool IsEnPassant; 
        }
        private ToolResolver() { }
        /// <summary>
        /// Create a schedule to execute a collection of tools independently.  Examines
        /// requires in the Tool's json and attempts to order to resolve in a single call
        /// rather than
        /// 
        /// LLM -> tool -> LLM tool ->etc.. finally resolve
        /// 
        /// LLM-> schedule tools -> exec -> return.
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>instance for scheduling an arbitrary collection of tool calls that are resolved in one swoop later</returns>
        public static ToolResolver CreateSchedule(string name) { return new ToolResolver(); }


        public bool HasScheduledTools => !this.Que.IsEmpty;

        public void ScheduleTool(JsonDocument PossibleTool, bool ExceptionOnInvalid = true)
        {
            var NodeWalk = PossibleTool.RootElement.EnumerateObject();


            bool has_function_name = false;
            bool has_arguments = false;

            string? FunctionName = string.Empty ;
            string? FunctionArguments = string.Empty;
            

            foreach (JsonProperty Prop in NodeWalk)
            {
                if (Prop.Name == "name")
                {
                    FunctionName = Prop.Value.GetString();
                    if (FunctionName is not null)
                        has_function_name = true;
                    continue;
                }
                if (Prop.Name == "arguments")
                {
                    FunctionArguments = Prop.Value.ToString();
                    if (FunctionArguments is not null)
                    {
                        has_arguments = true;
                    }
                    continue;
                }
                if ( (has_arguments == has_function_name) && (has_function_name == true))
                {
                    if  ( (string.IsNullOrEmpty(FunctionName) == false) && (string.IsNullOrEmpty(FunctionArguments) == false) ) 
                        break;
                }
                
            }

            // now pack
            if ((has_function_name == true) && (has_arguments == true))
            {
                if ( (!string.IsNullOrEmpty(FunctionName)) && (!string.IsNullOrEmpty(FunctionArguments)))
                    ScheduleTool(string.Empty, FunctionArguments, FunctionName);
            }
        }


 
        /// <summary>
        /// Schedule this tool or collect the function arguments passed to a tool previously added
        /// </summary>
        /// <param name="Tool"></param>
        /// <param name="HasInPassingTool">If the tool attempting to be scheduled if of type, <see cref="IButlerToolInPassing"/>, this is set to true</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ScheduleTool(ButlerStreamingToolCallUpdatePart Tool)
        {
            bool exists = false;
            var CheckExists = Que.Where(p => { return p.StreamingIndex == Tool.Index; });
            ToolTimeSlot? Entry;
            if (CheckExists.Any() == false)
            {
                Entry = new ToolTimeSlot();
                Entry.ToolName.Append(Tool.FunctionName);
                Entry.StreamingIndex = Tool.Index;
            }
            else
            {
                Entry = CheckExists.FirstOrDefault();
                exists = true;
            }

            if (Entry is not null)
            {
                Entry.ToolArgumentsPart.Append(Tool.FunctionArgumentsUpdate);
                if (Tool.ToolCallid is not null)
                    Entry.ID.Append(Tool.ToolCallid);
                if (!exists) { Que.Enqueue(Entry); }
            }
            else
            {
                throw new InvalidOperationException("Somehow the entry existed but the check returned null");
            }

        }


        /// <summary>
        /// Add this tool to be called when <see cref="RunSchedule()"/> is called
        /// </summary>
        /// <param name="ID">Usually will be <see cref="ChatToolCall.Id"/> or streaming variant. </param>
        /// <param name="Arguments">Usually will be <see cref="ChatToolCall.FunctionArguments"/> or streaming variant.  Note SHOULD BE JSON</param>
        /// <param name="Name">name of the tool to call.</param>
        public void ScheduleTool(string ID, string Arguments, string Name)
        {
            ArgumentNullException.ThrowIfNull(ID);
            ArgumentNullException.ThrowIfNull(Arguments);
            ArgumentNullException.ThrowIfNullOrEmpty(Name);

            ToolTimeSlot NewEntry = new();
            NewEntry.ID.Append(ID);
            NewEntry.ToolArgumentsPart.Append(Arguments);
            NewEntry.ToolName.Append(Name);
            Que.Enqueue(NewEntry);
        }


        /// <summary>
        /// Remove this tool to be scheduled to run. 
        /// </summary>
        /// <param name="Tool"></param>
        /// <exception cref="NotSupportedException">This is thrown.  One day, plan is letting cherry picking of individual tools</exception>
        public void RemoveTool(string ID)
        {
            throw new NotSupportedException("Once a tool schedule is added, currently there's no way to add remove it without clearing all scheduled tools");
        }

        /// <summary>
        /// blank the scheduler
        /// </summary>
        public void RemoveAllTool()
        {
            Que.Clear();
        }

        /// <summary>
        /// Butler3 Resolver for do now. Will await when done
        /// </summary>
        /// <param name="ToolDB"></param>
        /// <returns>object representing the finished task</returns>
        public async Task RunScheduleAsyncDoNow(ButlerToolBench ToolDB)
        {
            ArgumentNullException.ThrowIfNull(ToolDB);
            if ((Que.IsEmpty) && (!EmptyScheduleRunFine))
            {
                throw new NoToolScheduledException($"{this.GetType().Name} has no scheduled tools. This means the thing isn't actually gonna run anything when calling schedule. To turn this exception off, set flag EmptyScheduleRunFine true");
            }
            Task ret = RunScheduleAsync(ToolDB);
            await ret;
        }

        /// <summary>
        /// Butler5 Tool resolver
        /// </summary>
        /// <param name="ToolDB">At minimum your kit needs to implement <see cref="IButlerToolKitCallable"/> AND <see cref="IButlerToolKitQueryAndGet"/>. Or just use <see cref="IButlerToolBench"/></param>
        /// <param name="HasInPassing">Should one of the tools be of type <see cref="IButlerToolInPassing"/>, this this true</param>
        /// <returns></returns>
        public async Task RunScheduleAsync(IButlerToolKitQueryAndGet ToolCollection, ToolResolverTelemetryStats? Stats = null)
        {
            bool EnPassant = false;
            
            
            IButlerToolKitQueryAndGet? QueryToolKit = ToolCollection as IButlerToolKitQueryAndGet;
            IButlerToolKitCallable? CallableToolKit = ToolCollection as IButlerToolKitCallable;

            ArgumentNullException.ThrowIfNull(QueryToolKit, "The passed tool collection MUST Implement IButlerToolKitQueryAndGet interface in full");
            ArgumentNullException.ThrowIfNull(CallableToolKit, "The passed tool collection MUST Implement IButlerToolKitQueryAndGet interface in full");

            List<(string CallID, IButlerToolBaseInterface ToolUsed)>? UsedTools=null;
            if (Stats is not null)
            {
                UsedTools = new();
                Stats.ToolsUsed = UsedTools;
            }
            if ((Que.IsEmpty) && (!EmptyScheduleRunFine))
            {
                throw new NoToolScheduledException($"{this.GetType().Name} has no scheduled tools. This means the thing isn't actually gonna run anything when calling schedule. To turn this exception off, set flag EmptyScheduleRunFine true");
            }
            // collection of tasks for each ToolTime instance we resolve/run
            List<Task<ToolTimeSlot>> RunningRoles = new List<Task<ToolTimeSlot>>();
            while (!Que.IsEmpty)
            {
                // pop off each entry and create a task for it
                ToolTimeSlot? Entry;
                if (Que.TryDequeue(out Entry) && (Entry is not null))
                {
                    Task<ToolTimeSlot> tool = Task<ToolTimeSlot>.Run(async () =>
                    {
                    bool ok = false;
                    ButlerChatToolCallMessage? result = null;
#if DEBUG
                    bool FakeId = true;
                    if (FakeId && string.IsNullOrEmpty(Entry.ID.ToString()))
                    {
                        Entry.ID.Append("call-");
                        Entry.ID.Append(Guid.NewGuid().ToString());
                    }
#endif
                    IButlerToolBaseInterface? TargetTool = QueryToolKit.GetTool(Entry.ToolName.ToString());
                    try
                    {
                            // invoke the tool function. It's gonna set OK
                      
                        if (TargetTool is null)
                        {
                                throw new InvalidOperationException("ERROR: ToolName passed check OK but got null instead of the tool instance before call. ");
                        }
                        else
                        {
                                /* dear future maintainer
                                 * as C# currently don't like ref/out for async.
                                 * OK is changed for async
                                 * 
                                 * Sync version let the tool set set OK as normal or not and return a value seperate from that
                                 * 
                                 * Async version currently treats null return = bad time, not null = good time (ok is true).
                                 */
                                if (TargetTool is IButlerToolAsyncResolver TT)
                                {
                                    result = await CallableToolKit.CallToolFunctionAsync(TargetTool, Entry.ID.ToString(), Entry.ToolArgumentsPart.ToString());
                                    if (result is not null)
                                    {
                                        ok = true;
                                    }
                                    else
                                    {
                                        ok = false;
                                    }
                                }
                                else
                                {
                                    result = CallableToolKit.CallToolFunction(TargetTool, Entry.ID.ToString(), Entry.ToolArgumentsPart.ToString(), out ok);
                                }
                         if (Stats is not null)
                          {
                            UsedTools?.Add((Entry.ID.ToString(),TargetTool));
                           }
                        }
                        //result = ToolDB.CallToolFunction(Entry.ToolName.ToString(), Entry.ID.ToString(), Entry.ToolArgumentsPart.ToString(), out OK);
                    }
                    catch (Exception ex)
                    {
                        // grab the error  and add to the list
                        Entry.Failures.Add(ex);
                    }
                    finally
                    {
                     
                            Entry.IsEnPassant =  EnPassant = TargetTool is IButlerToolInPassing;
                            if (Stats is not null)
                            {
                                if (Entry.IsEnPassant)
                                {
                                    Stats.InPassingCount++;
                                }
                                if (TargetTool is IButlerCritPriorityTool)
                                {
                                    Stats.CritPriorityCount++;
                                }
                                Stats.ExceptionsCaught[Entry.ID.ToString()] = new List<Exception>();
                            }







                            // if OK. YAY
                            if (ok)
                            {
                                Entry.Results = result;
                            }
                            else
                            {
                                if (Stats is not null)
                                {
                                    Stats.ExceptionsCaught[Entry.ID.ToString()].AddRange(Entry.Failures);
                                }
                                // capture an exception> report
                                if (Entry.Failures.Count > 0)
                                    Entry.Results = new ButlerChatToolResultMessage(Entry.ID.ToString(), $"Tool Error: {Entry.Failures[Entry.Failures.Count - 1].Message}");
                                else
                                    Entry.Results = new ButlerChatToolResultMessage(Entry.ID.ToString(), $"Tool Error: {"The tool reported it did not have sucess."}");
                            }
                        }
                        return Entry;
                    });
                    
                    RunningRoles.Add(tool);
                }
            }

            // do the ye old await all. Is it perfect? Nope.
            await Task.WhenAll(RunningRoles);
  
      


            // move our resolved tools to the current pool.

            for (int i = 0; i < RunningRoles.Count; i++)
            {
                {
                    ResolvedTool.Add(RunningRoles[i].Result);
                }
            }
            
        
        }

        /*
        [Obsolete("DO NOT USE")]
        /// <summary>
        /// Run the scheduled tools in our class using this Tool Set
        /// </summary>
        /// <param name="ToolDB">The tool container we'll be calling</param>
        /// <returns>a task object to use</returns>
        /// <exception cref="ArgumentNullException">Will be thrown if ToolDB is null</exception>
        public Task RunSchedule(IButlerToolKitCallable ToolDB)
        {
            ArgumentNullException.ThrowIfNull(ToolDB);
            if ((Que.IsEmpty) && (!EmptyScheduleRunFine))
            {
                throw new NoToolScheduledException($"{this.GetType().Name} has no scheduled tools. This means the thing isn't actually gonna run anything when calling schedule. To turn this exception off, set flag EmptyScheduleRunFine true");
            }
            var ret = Task.Run(() =>
            {
                static void my_thread(object tool, IButlerToolKitCallable ToolDB)
                {
                    ToolTimeSlot arg = (tool as ToolTimeSlot);
                    if (arg != null)
                    {
                        try
                        {
                            bool K = false;
                            var Tool = ToolDB.CallToolFunction(arg.ToolName.ToString(), arg.ID.ToString(), arg.ToolArgumentsPart.ToString(), out K);
                            if (K)
                            {
                                arg.Results = Tool;
                            }
                        }
                        catch (Exception e)
                        {
                            arg.Results = new ButlerChatToolCallMessage(arg.ID.ToString(), $"Error: {arg.ToolName} did not work. Reason {e}");
                        }
                        finally
                        {
                            if (arg.Results == null)
                                arg.Results = new ButlerChatToolCallMessage(arg.ID.ToString(), $"Error: {arg.ToolName} did not get any data. Reason Unknown");
                        }

                    }
                }
                List<ToolTimeSlot> running = new();
                while (Que.IsEmpty == false)
                {
                    ToolTimeSlot? entry = null;
                    Que.TryDequeue(out entry);
                    if (entry != null)
                    {
                        running.Add(entry);
                        entry.Self = new Thread(() => { my_thread(entry, ToolDB); });
                    }
                }
                for (int step = 0; step < running.Count; step++)
                {
                    running[step].Self.Start();
                }

                Thread.Sleep(5);
                bool isAlive = true;

                while (isAlive)
                {
                    isAlive = false;
                    Thread.Sleep(5);
                    for (int i = 0; i < running.Count; i++)
                    {
                        if (running[i].Self.IsAlive == true)
                        {
                            isAlive = true;
                            break;
                        }
                    }
                    if (running.Count == 0)
                    {
                        isAlive = false;
                    }
                }

                foreach (ToolTimeSlot slot in running)
                {
                    ResolvedTool.Add(slot);
                }
                return;

            });
            return ret;
        }
        */


      

        public void PlaceInChatLog(IList<ButlerChatMessage> Messages)
        {
            PlaceInChatLog(Messages, false);
        }
        /// <summary>
        /// For streaming chats
        /// </summary>
        /// <param name="Messages"></param>
        /// <param name="MarkAsTemp">If true marks the tool pass and temporary, removing it from context window at end of ai turn</param>
        public void PlaceInChatLog(IList<ButlerChatMessage> Messages, bool MarkAsTemp)
        {


            // our general plan matches OpenAI because of familiarity. This routine grabs the tool call and results from our ToolTime class and puts out a Tool Call and Tool Result message in the passed list
            foreach (ToolTimeSlot tool in this.ResolvedTool)
            {
                
                
                ButlerChatToolResultMessage? Result = (ButlerChatToolResultMessage?)tool.Results;
                if (Result is not null)
                {

                    var callData = ButlerChatToolCallMessage.CreateFunctionToolCall(tool.ID.ToString(), tool.ToolName.ToString(), tool.ToolArgumentsPart.ToString());
                    Messages.Add(callData);
                    Result.Id = tool.ID.ToString();
                    Messages.Add(Result);
                    Result.Role = ButlerChatMessageRole.ToolResult;
                    Result.ToolName = tool.ToolName.ToString();
                    if (MarkAsTemp)
                    {
                        callData.IsTemporary = true;
                        Result.IsTemporary = true;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Casting error.  PlaceInChatLog was not able to convert ButlerChatToolCallMessage to a ButlerChatToolReplyMessage");
                }
            }
            this.ResolvedTool.Clear();
            return;
        }




        public int ScheduledToolCount => Que.Count;
        ConcurrentQueue<ToolTimeSlot> Que = new();



        /// <summary>
        /// Get the resolved list of tool calls in a read only form 
        /// </summary>
        /// <returns>if it works, the results of the each resulted tool call. Null on an issue</returns>
        public IReadOnlyList<ButlerChatToolCallMessage?> ResolvedToolResults()
        {
            return ResolvedTool.Select(s => s.Results).ToArray();
        }
        public int ResolvedToolCount => ResolvedTool.Count;

        ConcurrentBag<ToolTimeSlot> ResolvedTool = new();

        /// <summary>
        /// If false, attempting to run a schedule with no tools in it will throw exception
        /// </summary>
        public bool EmptyScheduleRunFine { get; set; } = false;
    }




}



