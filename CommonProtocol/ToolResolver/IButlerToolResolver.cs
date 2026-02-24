using ButlerSDK.ToolSupport.Bench;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace ButlerSDK.ToolSupport
{
    public interface IButlerToolResolver 
    {

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
        public static abstract IButlerToolResolver CreateSchedule(string name);


        public bool HasScheduledTools { get; set; }

        public void ScheduleTool(JsonDocument PossibleTool, bool ExceptionOnInvalid = true);


        /// <summary>
        /// Schedule this tool or collect the function arguments passed to a tool previously added
        /// </summary>
        /// <param name="Tool"></param>
        /// <param name="HasInPassingTool">If the tool attempting to be scheduled if of type, <see cref="IButlerToolInPassing"/>, this is set to true</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ScheduleTool(ButlerStreamingToolCallUpdatePart Tool);


        /// <summary>
        /// Add this tool to be called when <see cref="RunSchedule()"/> is called
        /// </summary>
        /// <param name="ID">Usually will be <see cref="ChatToolCall.Id"/> or streaming variant. </param>
        /// <param name="Arguments">Usually will be <see cref="ChatToolCall.FunctionArguments"/> or streaming variant.  Note SHOULD BE JSON</param>
        /// <param name="Name">name of the tool to call.</param>
        public void ScheduleTool(string ID, string Arguments, string Name);

        /// <summary>
        /// Remove this tool to be scheduled to run. 
        /// </summary>
        /// <param name="Tool"></param>
        /// <exception cref="NotSupportedException">This is thrown.  One day, plan is letting cherry picking of individual tools</exception>
        public void RemoveTool(string ID);

        /// <summary>
        /// blank the scheduler
        /// </summary>
        public void RemoveAllTool();

        /// <summary>
        /// Butler3 Resolver for do now. Will await when done
        /// </summary>
        /// <param name="ToolDB"></param>
        /// <returns>object representing the finished task</returns>
        public Task RunScheduleAsyncDoNow(IButlerToolBench ToolDB);

        /// <summary>
        /// Butler5 Tool resolver
        /// </summary>
        /// <param name="ToolDB">At minimum your kit needs to implement <see cref="IButlerToolKitCallable"/> AND <see cref="IButlerToolKitQueryAndGet"/>. Or just use <see cref="IButlerToolBench"/></param>
        /// <param name="HasInPassing">Should one of the tools be of type <see cref="IButlerToolInPassing"/>, this this true</param>
        /// <returns></returns>
        public Task RunScheduleAsync(IButlerToolKitQueryAndGet ToolCollection, ToolResolverTelemetryStats? Stats = null);




        public void PlaceInChatLog(IList<ButlerChatMessage> Messages);
        /// <summary>
        public void PlaceInChatLog(IList<ButlerChatMessage> Messages, bool MarkAsTemp);




        public int ScheduledToolCount { get; set; }




        /// <summary>
        /// Get the resolved list of tool calls in a read only form 
        /// </summary>
        /// <returns>if it works, the results of the each resulted tool call. Null on an issue</returns>
        public IReadOnlyList<ButlerChatToolCallMessage?> ResolvedToolResults();
        public int ResolvedToolCount { get; set; }

        

        /// <summary>
        /// If false, attempting to run a schedule with no tools in it will throw exception
        /// </summary>
        public bool EmptyScheduleRunFine { get; set; } 
    }

}