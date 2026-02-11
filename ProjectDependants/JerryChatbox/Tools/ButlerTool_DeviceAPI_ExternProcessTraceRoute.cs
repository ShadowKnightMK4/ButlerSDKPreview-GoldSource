using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK;
using ButlerSDK.Tools;

namespace ButlerSDK.Tools
{
    public class ButlerTool_DeviceAPI_ExternProcessTraceRoute : ButlerTool_DeviceAPI_ExternProcessBase
    {
        public override string ToolName => "TraceRoute";

        public override string ToolDescription => "Trace the IP routine to target url as an argument. Use the argument option";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""target"": {
            ""type"": ""string"",
            ""description"": ""THE IP or url to trace. ""
        }
    },
    ""required"": [ ""target"" ]
}";
            return json_template;
        }
        public ButlerTool_DeviceAPI_ExternProcessTraceRoute(IButlerVaultKeyCollection key): base(key) { }

        /// <summary>
        /// trace route gets 2 times long
        /// </summary>
        /// <returns></returns>
        protected override int GetWaitTimeout()
        {
            return base.GetWaitTimeout() * 2;
        }
        protected override string GetTargetProcessLocation()
        {
            string SysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!Directory.Exists(SysDir))
            {
                throw new InvalidOperationException("Somehow the system folder is not existing?");
            }
            if (SysDir.EndsWith(Path.DirectorySeparatorChar))
                SysDir = SysDir.TrimEnd(Path.DirectorySeparatorChar);
            if (SysDir.EndsWith(Path.AltDirectorySeparatorChar))
                SysDir = SysDir.TrimEnd(Path.AltDirectorySeparatorChar);

            return SysDir + Path.DirectorySeparatorChar + "tracert.exe";
        }

        protected override string BuildProcessArgument(string? FunctionCallArgs)
        {
            JsonElement Arg;
            if (FunctionCallArgs is not null)
            {
                if (JsonDocument.Parse(FunctionCallArgs).RootElement.TryGetProperty("target", out Arg))
                {
                    string? ret = Arg.ToString();
                    if (ret is null)
                    {
                        throw new ArgumentException("Couldn't make the arguments for the call. Json target null?");
                    }
                    return ret;
                }
                else
                {
                    throw new ArgumentException("Couldn't make the arguments for the call. Json Parsing error");
                }
            }
            else
            {
                throw new ArgumentException("Note: BuildProcessArguments() requires non null JSON with target property");
            }
            
        }



      
    }
}
