using ButlerToolContract;

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
}