using ButlerLLMProviderPlatform.Protocol;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ButlerPostProcessing;
using ButlerSDK.Core;
using ButlerSDK.Debugging;
using ButlerSDK.ToolSupport;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.Core
{

    public class Butler : ButlerBase
    {
        protected readonly IButlerPostProcessorHandler? PostProcessing;
        protected readonly IButlerChatPreprocessor? PreProcessing;

        /// <summary>
        /// What tool self reported permissions are allowed. A <see cref="SecurityException"/> will trigger on added tool
        /// </summary>
        public ToolSurfaceScope AllowedPermissions => base.ToolSurfaceScope;
        /// <summary>
        /// If <see cref="AutoSysPromptToday"/> is true, this method is called to insert today's date/time as a system prompt each turn.
        /// </summary>
        /// <remarks>This method is overridable if you want to insert something else</remarks>
        protected virtual void HandleAutoSysPromptToday()
        {
            if (AutoSysPromptToday)
            {
                string msg = $"Date: {DateTime.Now.ToLongDateString()} Time: {DateTime.Now.ToLocalTime()}";
                ButlerSystemChatMessage Today = new ButlerSystemChatMessage(msg);
                LogTap?.LogString($"Injecting \"{msg}\" as system prompt");
                Today.IsTemporary = true;
                ChatCollection.Add(Today);
            }
        }


        /// <summary>
        /// If true, each turn gets a call to <see cref="HandleAutoSysPromptToday"/>, which by default adds today's date/time as a temporary system prompt."/>
        /// </summary>
        public bool AutoSysPromptToday = true;

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="Key">This is used to source needed API keys needed.  </param>
        /// <param name="Provider">An implementation of the provider that Butler will use to communicate with the LLM of your choice - see <see cref="IButlerLLMProvider"/> </param>
        /// <param name="Opts">This can be null if the provider <see cref="IButlerChatCreationProvider"/> has a <see cref="IButlerChatCreationProvider.DefaultOptions"/> that is not null</param>
        /// <param name="ModelChoice">This is the model requested. It's passed as its and cannot be null </param>
        /// <param name="KeyVar">Related to <see cref="IButlerVaultKeyCollection"/> Keys instance. Butler will require the key named that.</param>
        /// <param name="PostProcessor">Optional post processor handler for guiding LLMs as they product data - see <see cref="IButlerPostProcessorHandler"/> and <see cref="ToolPostProcessing"/> for a way to use it</param>
        /// <param name="PPR">The pre processor. Optional. Provider can actually ignore if wanted - they need to exception if so. Before Translation from butler by provider, this is called to possibly alter a copy of the message</param>
        public Butler(IButlerVaultKeyCollection Key, IButlerLLMProvider Provider, IButlerChatCompletionOptions? Opts, string ModelChoice, string KeyVar, IButlerPostProcessorHandler? PostProcessor = null, IButlerChatPreprocessor? PPR = null) :
            base(Key, Provider, Opts, ModelChoice, KeyVar)
        {
            this.PostProcessing = PostProcessor;
            this.PreProcessing = PPR;
        }

        /// <summary>
        /// Get this provider's tool mode
        /// </summary>
        /// <param name="Provider">Provider to ask</param>
        /// <param name="Mode">Where to store the result</param>
        /// <remarks>Default mode is <see cref="IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation"/> aka OpenAi cloud</remarks>
        internal static void _QueryLLMToolMode(IButlerLLMProvider Provider, out IButlerLLMProvider.ToolProviderCallBehavior Mode)
        {
            if (Provider is IButlerLLMProviderToolRequests ChosenProvider)
            {
                Mode = ChosenProvider.GetToolMode();
            }
            else
            {
                Mode = IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation;
            }
        }

        /// <summary>
        /// An awaitable routine that triggers tool calls if any are scheduled in the resolver
        /// </summary>
        /// <param name="Resolver">If null, this routine does nothing</param>
        /// <param name="Stats">Optional stat class <see cref="ToolResolverTelemetryStats"/> to get insight in execution</param>
        /// <returns>Task to await</returns>

        internal async Task _TriggerToolCall(ToolResolver? Resolver, ToolResolverTelemetryStats? Stats)
        {
            if (Resolver is not null)
            {
                if (Resolver.HasScheduledTools)
                {
                    LogTap?.LogString("Tool Resolver object has scheduled tools. Triggering and awaiting.");
                    await Resolver.RunScheduleAsync(base.ToolSet, Stats);
                    Resolver.PlaceInChatLog(ChatCollection, false);
                    LogTap?.LogString("Placed tool results in chat log");
                }
            }
        }


        /// <summary>
        /// If passed resolver is null, create it
        /// </summary>
        /// <param name="Resolver">ref to Resolver to create</param>
        internal void _EnsureResolverIsActive(ref ToolResolver? Resolver)
        {
                if (Resolver is null)
                {
                    LogTap?.LogString("Created the Tool Resolver object");
                    Resolver = ToolResolver.CreateSchedule("ResolveMe");
                }
                else
                {
                    LogTap?.LogString("Not creating Tool Resolver object, it already exists.");
                }
            
            
        }

        internal class _HandleToolPrologArgs
        {
            /// <summary>
            /// THe resolver to schedule tool calls in
            /// </summary>
            public ToolResolver? Resolver;
            /// <summary>
            /// Gets set to true if a tool call was triggered in the packet <see cref="_HandleAToolCall(ButlerStreamingChatCompletionUpdate, _HandleToolPrologArgs)"/>
            /// </summary>
            public bool TriggeredCall;
            /// <summary>
            /// Gets set The provider mode for tool streaming
            /// </summary>
            public IButlerLLMProvider.ToolProviderCallBehavior ProviderBehavior;
            /// <summary>
            /// The optional caller supplied stats object to get telemetry on tool calls
            /// </summary>
            public ToolResolverTelemetryStats? ResolverStatus;
        }

        /// <summary>
        /// Examine the tool calls in the passed packet. If any, schedule them in the resolver and trigger them if needed. This does trigger calls if Provider is OneShot mode
        /// </summary>
        /// <param name="PosToolCall">a received streamed packet that might contain a tool call</param>
        /// <param name="Args">Arguments to parse</param>
        /// <returns></returns>
        internal async Task _HandleAToolCall(ButlerStreamingChatCompletionUpdate PosToolCall, _HandleToolPrologArgs Args)
        {
            // no call until we know we have tool calls
            // and get the provider mode
            Args.TriggeredCall = false;
            _QueryLLMToolMode(this.Provider, out Args.ProviderBehavior);

            if ((PosToolCall.ToolCallUpdates is not null) && (PosToolCall.ToolCallUpdates.Count > 0))
            {
                // set up resolver if needed and also set args to triggered call
                _EnsureResolverIsActive(ref Args.Resolver);
                Args.TriggeredCall = true;
                switch (Args.ProviderBehavior)
                {
                    case IButlerLLMProvider.ToolProviderCallBehavior.OneShot:
                        {
                            /* Once shot mode is the whole thing is a full call. Nothing to buffer.
                             Trigger tool call, and pack results*/
                            List<string>? Names = null;
                            if (LogTap is not null)
                                Names = new List<string>();

                            int ToolCount = 0;
                            LogTap?.LogString("Provider reports its OneShot (aka one packet per request). Querying tools to schedule and then Resolve them.");
                            foreach (var PosCall in PosToolCall.ToolCallUpdates)
                            {
                                ToolCount++;
                                Names?.Add(PosCall.FunctionName);
                                Args.Resolver!.ScheduleTool(PosCall);
                            }
                            LogTap?.LogString($"Queried {ToolCount} number of tools.");
                            LogTap?.LogString($"Queried these tools: {Names?.ToArray()}");

                            await _TriggerToolCall(Args.Resolver, Args.ResolverStatus);
                            if (Args.ResolverStatus is not null)
                            {
                                if (Args.ResolverStatus.ToolsUsed is not null)
                                {
                                    this.ChatCollection.AddPostToolCallFollowup(Args.ResolverStatus.ToolsUsed, string.Empty);
                                }
                            }
                            
                            break;
                        }
                    case IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation:
                        {
                            /* this is a  streamed call
                             * Pass to the scheduler and wait for more packets
                             */
                            int ToolCount = 0;
                            LogTap?.LogString("Provider reports its StreamAccumulation (aka more than  packet per request). Querying tools to schedule");

                            foreach (var PosCall in PosToolCall.ToolCallUpdates)
                            {
                                ToolCount++;
                                Args.Resolver!.ScheduleTool(PosCall);
                            }
                            break;
                        }
                }
            }
            else
            {
                /* why this is here The packet is not holding any 
                 tools.
                
                 And we need to treat that as end of the Stream variant of provider*/
                if (Args.Resolver is not null)
                {
                    if (Args.ProviderBehavior == IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation)
                    {
                        if (Args.Resolver is not null)
                        {
                            if (Args.Resolver.HasScheduledTools)
                            {
                                // LogTap is called in this
                                LogTap?.LogString("Provider reports its StreamAccumulation (aka more than  packet per request). Attempting to resolve scheduled tools");
                                await _TriggerToolCall(Args!.Resolver!, Args.ResolverStatus);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A buffer dump is requested. by CounterMeasures object. Forward all queued packets to HandlerQue
        /// </summary>
        /// <param name="CounterMeasures">CounterMeasures option. Note this is assumed not null because the routine that should call it preps for that</param>
        /// <param name="LogHelp">what mode to past into the LogHelp string for the DebugTap </param>
        /// <param name="HandlerQue">Queue we're routing packets too</param>
        internal void _DumpBufferHandler(IButlerPostProcessorHandler CounterMeasures, string LogHelp, Queue<ButlerStreamingChatCompletionUpdate> HandlerQue)
        {
            bool Warned = false;
            int que_size = CounterMeasures.QueueSize;
            LogTap?.LogString($"CounterMeasures object wants \"{LogHelp}\" mode. Routing CounterMeasure buffer to Handler callback\r\n");
            LogTap?.LogString($"Que size: {que_size}\r\n");
            /* a bit of the logic below is this:
             * try to pop.  Assume que size is accurate. Track if it doesn't actually change (warn user on debug tap)
             * and finally bail on invalid operation exception (que empty).  This is to prevent inf loop if que size is wrong
             */
            while (CounterMeasures.QueueSize > 0)
            {
                try
                {
                    var part = CounterMeasures.DeQueueBuffer();
                    if (part is not null)
                    {
                        HandlerQue.Enqueue(part);
                        //AssistantTurnReply.Append(part);
                    }
                    if (CounterMeasures.QueueSize >= que_size)
                    {
                        if (!Warned)
                            LogTap?.LogString("WARNING: CounterMeasure is not tracking que size properly. This means possible error in object, may be inf loop (will be broken on InvalidOperationException)");
                        Warned = true;
                    }
                }
                catch (InvalidOperationException)
                {
                    LogTap?.LogString("Warning: InvalidOperationException triggered. This can be cause query is empty and the size isn't accurate. Exiting dump buffer mode\r\n");
                    break;
                }

            }
        }

        /// <summary>
        /// common code to gracefully check for null before calling <see cref="TrenchCoatChatCollection.AddPostToolCallFollowup(IReadOnlyList{ValueTuple{string, ButlerToolContract.IButlerToolBaseInterface}}, string)"/>
        /// </summary>
        /// <param name="ToolContext"></param>
        internal void _PostToolCallTriggerHandler(_HandleToolPrologArgs ToolContext)
        {
            if (ToolContext.ResolverStatus is not null)
            {
                if (ToolContext.ResolverStatus.ToolsUsed is not null)
                {
                    ChatCollection.AddPostToolCallFollowup(ToolContext.ResolverStatus.ToolsUsed, string.Empty);
                }
            }
        }
        /// <summary>
        /// Selects and outputs the appropriate chat client instance for the current streaming session.
        /// </summary>
        /// <param name="Target">When this method returns, contains the selected chat client instance to be used for the session. This
        /// parameter is passed uninitialized.</param>
        /// <exception cref="InvalidOperationException">Thrown if a chat client cannot be determined for the streaming session.</exception>
        internal void _SelectClient(out IButlerChatClient Target)
        {
            if (this.ModelChoice is not null)
            {
                try
                {
                    Target = base.ChatFactory.GetChatClient(ModelChoice, this.MainOptions, PreProcessing)!;
                    // the ! justified is because later got throws exception if this is null
                }
                catch (ModuleNotFoundException)
                {
                    Target = base.Chat!;
                }
            }
            else
            {
                Target = base.Chat!; // the ! justified is because later got throws exception if this is null
            }
            if (Target is null)
            {
                // the later code from the comments
                throw new InvalidOperationException("Unable to lock in the chat client for a streaming session");
            }
        }

        public void SetLogger(ButlerTap Logging)
        {
            this.LogTap = Logging;
        }
        ButlerTap? LogTap;


        /// <summary>
        /// A Synchronous override that forwards calls <see cref="StreamResponseAsync(ChatMessageStreamHandler, IButlerPostProcessorHandler?, bool, int, int, CancellationToken)"/>
        /// </summary>
        /// <param name="Handler">callback</param>
        /// <param name="SkipAddingLLMResponse"></param>
        /// <returns>Returns the finish reason</returns>
        /// <remarks>THIS CLASS will use that passed in the constructor earlier for post processing </remarks>
        public override ButlerChatFinishReason? StreamResponse(ChatMessageStreamHandler Handler, bool SkipAddingLLMResponse = false)
        {
            var task = StreamResponseAsync(Handler, this.PostProcessing, SkipAddingLLMResponse, default).ConfigureAwait(false).GetAwaiter().GetResult();

            return task;
        }


        /// <summary>
        /// This enum is how <see cref="StreamResponseAsync(ChatMessageStreamHandler, IButlerPostProcessorHandler?, bool, int, int, CancellationToken)"/> interprets the recovery options the provider indicated
        /// </summary>
        internal enum NetworkErrorAction
        {
            /// <summary>
            /// Default state
            /// </summary>
            NoError,
            /// <summary>
            /// Do no action
            /// </summary>
            None ,// *Elsa voice* let it go (and propagate to the consumer of the library*
            SleepAction , // recovery is sleep x and then goto reset via a label.

            SomethingBadHappened,
            /// <summary>
            /// Do not attempt recovery and try again. Send the exception up the pipe
            /// </summary>
            DoNotRecover,
            // more flags as needed
        }
        /// <summary>
        /// Stream Async response from LLM, with optional post processing handler for countermeasures
        /// </summary>
        /// <param name="Handler">callback to pass packets to for caller</param>
        /// <param name="CounterMeasures">Optional Quality control handler <see cref="ToolPostProcessing"/></param>
        /// <param name="SkipAddingLLMResponse">if true, the ai's final reply is *not* added. </param>
        /// <param name="MaxRemedials">Each time CounterMeasures triggers a do it again, it costs a remedial. No more than MaxRemedials will fire</param>
        /// <param name="NetworkErrorMaxRetries"></param>
        /// <param name="cancelMe">cancel token</param>
        /// <returns>It's a task that's the finish reason of the final LLM turn</returns>
        public async Task<ButlerChatFinishReason?> StreamResponseAsync(ChatMessageStreamHandler Handler, IButlerPostProcessorHandler? CounterMeasures = null, bool SkipAddingLLMResponse = false, int MaxRemedials = 5, int NetworkErrorMaxRetries = 5, CancellationToken cancelMe = default)
        {

            bool TurnOver = false;
            Queue<ButlerStreamingChatCompletionUpdate> HandlerQue = new();
            IButlerChatClient preTargetClient;
            ButlerChatFinishReason? FinishReason = null;
            // set target client to either model choice (if not null) or base.Chat
            // trigger exception if null
            _SelectClient(out preTargetClient);
            IButlerLLMProvider.ToolProviderCallBehavior ToolCallAction;

            _QueryLLMToolMode(this.Provider, out ToolCallAction);

            // the reply we're building
            ButlerMessageStitcher AssistantTurnReply = new();

            // the tool context object syncing the various internal routines in this class
            _HandleToolPrologArgs ToolContext = new();

            // the class to gain tool execution data from the run
            ToolContext.ResolverStatus = new ToolResolverTelemetryStats();

            // tracks how many remedial attempts
            int ErrorRemedial = 0;

            // tracks how many times
            int CurrentNetworkAttempts = 0;

            // tracks if a tool is called or not
            bool ToolTriggered = false;

            // the current action to do post attempt token (where that at is around line 524 or if that's out of date search string "NetworkRecoveryCODE1235"
            NetworkErrorAction NetworkErrorStateHandling = NetworkErrorAction.NoError;// exception triggers we can't handle from update (aka sdk throws hands ect)
            LogTap?.BeginLog();
            LogTap?.LogString("AI TURN STARTING: \r\n");

            // we save this for QOS turn (if implemented)
            int OriginalContextWindowStart = ChatCollection.RunningContextWindowCount - 1;
            while (!TurnOver)
            {
                if (AutoSysPromptToday)
                {

                    HandleAutoSysPromptToday();
                }
                AssistantTurnReply = new();

            ProviderErrorHandlerMark: // if the provider reports continuing, we execute the request and goto here.
                                      // provider should *not* modify butler state/ect/side effects - just report the action
                                      // to do.
                var StreamWalker = preTargetClient.CompleteChatStreamingAsync(this.ChatCollection, this.MainOptions, cancelMe);
                ToolTriggered = false;
                FinishReason = null;
                NetworkErrorStateHandling = NetworkErrorAction.NoError;
                try
                {
                    await foreach (var StreamPart in StreamWalker)
                    {
                        LogTap?.LogStreamingUpdate(StreamPart);
                        if (StreamPart.IsEmpty())
                        {
                            LogTap?.LogString("Dropped LLM Part that's empty\r\n");
                            continue;
                        }
                        if (StreamPart.FinishReason is not null)
                        {

                                FinishReason = StreamPart.FinishReason;
                            
                            if (FinishReason == ButlerChatFinishReason.ToolCalls)
                            {
                                LogTap?.LogString("Detected Tool Call trigger. FinishReason is ToolCalls\r\n");
                                ToolTriggered = true;
                                
                            }
                            else
                            {
                                LogTap?.LogString($"Finish Reason: \"{Enum.GetName(typeof(ButlerChatFinishReason), FinishReason)}\"");
                            }

                        }



                        // tools seen and found by provider's underyling sdk, that same provider received get picked up here
                        {
                            if ((StreamPart.ToolCallUpdates is not null) && (StreamPart.ToolCallUpdates.Count > 0))
                            {
                                LogTap?.LogString("Provider SDK saw the tool call then passed it to butler. Que for execution");
                                ToolTriggered = true;
                                await _HandleAToolCall(StreamPart, ToolContext);
                            }
                            else
                            {
                                if ((ToolTriggered == true))
                                {
                                    _EnsureResolverIsActive(ref ToolContext.Resolver);
                                    if (ToolCallAction == IButlerLLMProvider.ToolProviderCallBehavior.OneShot)
                                    {
                                        if (ToolContext.Resolver!.HasScheduledTools)
                                        {
                                            LogTap?.LogString("Provider is one shot (sends tool calls in one packet). Executing them now.");
                                            await _TriggerToolCall(ToolContext.Resolver, ToolContext.ResolverStatus);
                                            LogTap?.LogTelemetryStats(ToolContext.ResolverStatus!); // ! justified"

                                            ToolContext.Resolver.PlaceInChatLog(ChatCollection);
                                            _PostToolCallTriggerHandler(ToolContext);
                                            TurnOver = false;
                                        }
                                    }
                                    else if (ToolCallAction == IButlerLLMProvider.ToolProviderCallBehavior.StreamAccumulation)
                                    {
                                        if ((StreamPart.EditableToolCallUpdates is null) || (StreamPart.EditableToolCallUpdates.Count == 0))
                                        {
                                            if (ToolContext.Resolver!.HasScheduledTools)
                                            {

                                                LogTap?.LogString("Provider is StreamAccumulation (sends tool calls in several packets). Seems done. Executing them now.");
                                                await _TriggerToolCall(ToolContext.Resolver, ToolContext.ResolverStatus);
                                                LogTap?.LogTelemetryStats(ToolContext.ResolverStatus);

                                                ToolContext.Resolver.PlaceInChatLog(ChatCollection);
                                                
                                                _PostToolCallTriggerHandler(ToolContext);

                                                TurnOver = false;
                                            }
                                        }
                                    }

                                }

                            }
                        }

                        if (CounterMeasures is not null)
                        {
                            LogTap?.LogString($"Sending Packet to CounterMeasures Object.\r\n");
                            var action = CounterMeasures.ProcessReply(StreamPart, ToolTriggered);
                            string? enumTypeName = string.Empty;
                            if (LogTap is not null)
                            {
                                enumTypeName = Enum.GetName(typeof(IButlerPostProcessorHandler.PostProcessorAction), action);
                                if (enumTypeName is null) enumTypeName = string.Empty;
                            }
                            else
                                enumTypeName = string.Empty;
                            switch (action)
                            {
                                case IButlerPostProcessorHandler.PostProcessorAction.Buffered:
                                    {
                                        LogTap?.LogString($"CounterMeasures object wants \"{enumTypeName}\" mode. Not sending packet to ui Handler callback.\r\n");
                                        break;
                                    }
                                case IButlerPostProcessorHandler.PostProcessorAction.Discard:
                                    {
                                        LogTap?.LogString($"CounterMeasures object wants \"{enumTypeName}\" mode. Never sending packet to ui Handler callback\r\n");
                                        break;
                                    }
                                case IButlerPostProcessorHandler.PostProcessorAction.DumpBuffer:
                                    {
                                        _DumpBufferHandler(CounterMeasures, enumTypeName, HandlerQue);
                                        break;
                                    }
                                case IButlerPostProcessorHandler.PostProcessorAction.InvalidateAndAppend:
                                    {
                                        LogTap?.LogString($"CounterMeasures object wants \"{enumTypeName}\" mode. Discarding current ai message being built and restarting with this part.");
                                        AssistantTurnReply = new();
                                        HandlerQue.Clear();
                                        HandlerQue.Enqueue(StreamPart);
                                        break;
                                    }
                                case IButlerPostProcessorHandler.PostProcessorAction.PassThru:
                                    {
                                        LogTap?.LogString($"CounterMeasures object wants \"{enumTypeName}\" mode. Querying to send to handler and Stitcher.");
                                        HandlerQue.Enqueue(StreamPart);

                                        break;
                                    }
                            }

                        }
                        else
                        {
                            LogTap?.LogString($"No CounterMeasures Object: immediate pass thru to handler and add to Stitcher");
                            AssistantTurnReply.Append(StreamPart);
                            if (Handler is not null) Handler(StreamPart, ChatCollection);

                        }
                    }
                }
                catch (Exception ex)
                {
                    // marker comment "NetworkRecoveryCODE1235" to help people know where the recover logic is at



                    int SleepTime = 0;
                    NetworkErrorStateHandling = NetworkErrorAction.SomethingBadHappened;
                    if (this.Provider is IButlerLLMProvider_RecoverOptions Recovery)
                    {
                        if (ex is AggregateException ClownCar)
                        {
                            if (Recovery.StreamingErrorHandler(ClownCar, true, out SleepTime))
                            {
                                NetworkErrorStateHandling = NetworkErrorAction.SleepAction;
                            }
                            else
                            {
                                NetworkErrorStateHandling = NetworkErrorAction.DoNotRecover;
                            }
                        }
                        else
                        {
                            if (Recovery.StreamingErrorHandler(ex, true, out SleepTime))
                            {
                                NetworkErrorStateHandling = NetworkErrorAction.SleepAction;
                            }
                            else
                            {
                                NetworkErrorStateHandling = NetworkErrorAction.DoNotRecover;
                            }

                        }
                    }

                    if ( (NetworkErrorStateHandling == NetworkErrorAction.SleepAction))
                    {
                        if ( CurrentNetworkAttempts < NetworkErrorMaxRetries)
                        {
                            NetworkErrorStateHandling++;
                            LogTap?.LogString($"Attempting recoverable C# exception in mid ai turn. Used {CurrentNetworkAttempts} out of a max of {NetworkErrorMaxRetries} so far");
                            await Task.Delay(SleepTime, cancelMe);
                            goto ProviderErrorHandlerMark;
                        }
                        else
                        {
                            LogTap?.LogString($"Provider indicated C# exception recoverable BUT we're out of recovery attempts. Currently at {CurrentNetworkAttempts} out of a max of {NetworkErrorStateHandling} so are. Passing exception up the chain");
                            throw;
                        }

                    }
                    
                }

                if (CounterMeasures is not null)
                {
                    _DumpBufferHandler(CounterMeasures, "\"End of Stream Dump buffer Mode\"", HandlerQue);
                    _EnsureResolverIsActive(ref ToolContext.Resolver);

                    LogTap?.LogString($"Alerting CounterMeasures that LLM finished Streaming reply (EOS).\r\n");
                    var action = CounterMeasures.EndOfStreamAlert(FinishReason, ChatCollection, AssistantTurnReply.GetMessage(ButlerChatMessageRole.Assistant), MainOptions, ToolTriggered);
                    switch (action)
                    {
                        case IButlerPostProcessorHandler.EndOfAiStreamAction.None:
                            {
                                LogTap?.LogString($"EOS says OK. Butler is discarding temporary messages. If not a tool call turn, LLM turn is over\r\n");
                                this.ChatCollection.RemoveTemporaryMessages();
                                if (FinishReason == ButlerChatFinishReason.ToolCalls)
                                {

                                    TurnOver = false;
                                }
                                else
                                    TurnOver = true;
                                break;
                            }
                        case IButlerPostProcessorHandler.EndOfAiStreamAction.Triggered:
                            {
                                if (ErrorRemedial < MaxRemedials)
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed. Discarding Temporary messages <<< TURN IS NOT OVER>>. Calling Remedial and doing another pass thru in the ai turn. Current Tries: {ErrorRemedial} out of {MaxRemedials}\r\n");
                                    ErrorRemedial++;
                                    TurnOver = false;
                                    this.ChatCollection.RemoveTemporaryMessages();
                                    HandlerQue.Clear();
                                    CounterMeasures.Remedial(this.ChatCollection, ToolContext.Resolver!, this.ToolSet); //! justified see ensure resolver thing is active earlier in this if block
                                }
                                else
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed BUT WE'RE OUT OF Remedial Turns. Ending loop. Adding [CIRCUIT BREAK] Current Tries: {ErrorRemedial} out of {MaxRemedials}\r\n");
                                    TurnOver = true;
                                    ButlerSystemChatMessage msg = new("[CIRCUIT BREAK] You were unable to help the user this time. Report as much.");
                                    msg.IsTemporary = true;
                                    HandlerQue.Clear();
                                    this.ChatCollection.Add(msg);
                                }
                                break;
                            }
                        case IButlerPostProcessorHandler.EndOfAiStreamAction.TriggeredAndDiscard:
                            {
                                if (ErrorRemedial < MaxRemedials)
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed. Discarding Temporary messages <<< TURN IS NOT OVER>>. Calling Remedial and doing another pass thru in the ai turn. Current Tries: {ErrorRemedial} out of {MaxRemedials}");
                                    ErrorRemedial++;
                                    TurnOver = false;
                                    AssistantTurnReply = new();
                                    HandlerQue.Clear();
                                    this.ChatCollection.RemoveTemporaryMessages();
                                    CounterMeasures.Remedial(this.ChatCollection, ToolContext.Resolver!, this.ToolSet); //! justified see ensure resolver thing is active earlier in this if block
                                }
                                else
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed BUT WE'RE OUT OF Remedial Turns. Ending loop. Adding [CIRCUIT BREAK]Current Tries: {ErrorRemedial} out of {MaxRemedials}");
                                    TurnOver = true;
                                    ButlerSystemChatMessage msg = new("[CIRCUIT BREAK] You were unable to figure a solution. Report as much. '[ERROR]'");
                                    msg.IsTemporary = true;
                                    this.ChatCollection.Add(msg);
                                }
                                break;
                            }
                        case IButlerPostProcessorHandler.EndOfAiStreamAction.TriggeredAndAppendTemp:
                            {
                                if (ErrorRemedial < MaxRemedials)
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed. Discarding Temporary messages <<< TURN IS NOT OVER>>. Calling Remedial and doing another pass thru in the ai turn. Current Tries: {ErrorRemedial} out of {MaxRemedials}. Adding message so far as a temp one for remedial to examine");
                                    ErrorRemedial++;
                                    TurnOver = false;
                                    var msg = AssistantTurnReply.GetMessage(ButlerChatMessageRole.System);
                                    msg.IsTemporary = true;
                                    this.ChatCollection.RemoveTemporaryMessages();
                                    ChatCollection.Add(msg);
                                    HandlerQue.Clear();
                                    AssistantTurnReply = new();
                                    CounterMeasures.Remedial(this.ChatCollection, ToolContext.Resolver!, this.ToolSet); //! justified see ensure resolver thing is active earlier in this if block
                                }
                                else
                                {
                                    LogTap?.LogString($"EOS says Remedial is needed BUT WE'RE OUT OF Remedial Turns. Ending loop. Adding [CIRCUIT BREAK]Current Tries: {ErrorRemedial} out of {MaxRemedials}");
                                    TurnOver = true;
                                    ButlerSystemChatMessage msg = new("[CIRCUIT BREAK] You were unable to figure a solution. Report as much. '[ERROR]'");
                                    msg.IsTemporary = true;
                                    this.ChatCollection.Add(msg);
                                }
                                break;
                            }
                    }
                }
                else
                {
                    TurnOver = true;
                }

                if (ToolContext.Resolver is not null)
                {
                    LogTap?.LogString("Checking if last ditch tool resolving.");
                    this.ChatCollection.RemoveTemporaryMessages();
                    //if (ToolCallAction == IButlerLLMProvider.ToolProviderCallBehav.StreamAccumulation)
                    {
                        if (ToolContext.Resolver.HasScheduledTools)
                        {
                            LogTap?.LogString("There are unresolved tools. calling them and doing another ai turn.");
                            await _TriggerToolCall(ToolContext.Resolver, ToolContext.ResolverStatus);
                            ToolContext.Resolver.PlaceInChatLog(ChatCollection);
                            _PostToolCallTriggerHandler(ToolContext);
                            TurnOver = false;
                        }
                    }

                }

                if (TurnOver is true)
                {

                    LogTap?.LogString("Ai Turn over. dumping any remaining message parts to UI Handler callback.");
                    int QueSize = HandlerQue.Count;
                    // this puts the known good turn to the handler and message
                    while (QueSize > 0)
                    {
                        var part = HandlerQue.Dequeue();
                        if (part is not null)
                        {
                            if (Handler is not null) Handler(part, ChatCollection);
                            AssistantTurnReply.Append(part);
                        }
                        QueSize--;
                    }



                    if (CounterMeasures is not null)
                    {
                        _DumpBufferHandler(CounterMeasures, "End of Turn Counter Measures buffer clear", HandlerQue);
                    }


                    var Msg = AssistantTurnReply.GetMessage(ButlerChatMessageRole.Assistant);
                    if ((Msg.Content is null) || (Msg.Content.Count == 0) || (string.IsNullOrWhiteSpace(Msg.GetCombinedText())))
                    {

                        if (FinishReason == ButlerChatFinishReason.ToolCalls)
                        {
                            LogTap?.LogString("Turn over via tool call per LLM. Results added to list earlier. Turn is NOT OVER");
                            TurnOver = false;
                        }
                        else
                        {
                            this.ChatCollection.RemoveTemporaryMessages();

                            if (ErrorRemedial < MaxRemedials)
                            {
                                LogTap?.LogString("FINALE Kick. LLM Seemed to not respond. Attempted to force with a remedial");
                                TurnOver = false;
                                ErrorRemedial++;
                                ButlerSystemChatMessage msg = new ButlerSystemChatMessage("[DIRECTIVE] PLEASE RESPOND TO USER REQUEST!");
                                ChatCollection.Add(msg);
                            }
                            else
                            {
                                LogTap?.LogString("FINALE Kick can't be done. LLM Seemed to not respond. Out of  force with a remedial\r\n");
                            }
                        }

                    }
                    else
                    {
                        if (FinishReason != ButlerChatFinishReason.ToolCalls)
                        {
                            LogTap?.LogString("Message seems ok - not blank\r\n");
                            // here
                            ChatCollection.Add(Msg);
                            if (CounterMeasures is IButlerPostProcessorQOS QOS)
                            {
                                if (QOS.QosEnabled)
                                {
                                    LogTap?.LogString($"Detected QOS Implementation, handing it off for one final pass. RunningContextWindow Start {OriginalContextWindowStart}. Ending (before temp messages removed) {ChatCollection.RunningContextWindowCount} ");
                                    var ReplyMessage = await QOS.FinalQOSCheck(Provider, preTargetClient, ChatCollection, OriginalContextWindowStart, ChatCollection.RunningContextWindowCount);
                                    if (ReplyMessage is not null)
                                    {
                                        ChatCollection[ChatCollection.Count] = ReplyMessage;
                                    }
                                }
                            }
                        }
                        else
                        {
                            TurnOver = false;
                        }
                    }
                    LogTap?.LogString($"This ai turn used  {ErrorRemedial} Remedial corrections of a max of {MaxRemedials} turns to to reply\r\n");



                }

            }




            LogTap?.LogString("Ai Turn over. Cleaning temporary messages \r\n");
            this.ChatCollection.RemoveTemporaryMessages();
            return FinishReason;
        }



    }
}
