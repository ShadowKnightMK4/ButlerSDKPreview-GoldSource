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
    public class ButlerTool_DeviceAPI_ExternProcessPing : ButlerTool_DeviceAPI_ExternProcessBase
    {
        public override string ToolName => "Ping";

        public override string ToolDescription => "Ping target url as an argument. Use the argument option";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            const string json_template = @"{
    ""type"": ""object"",
    ""properties"": {
        ""target"": {
            ""type"": ""string"",
            ""description"": ""THE IP or url to ping. ""
        }
    },
    ""required"": [ ""target"" ]
}";
            return json_template;
        }
        public ButlerTool_DeviceAPI_ExternProcessPing(IButlerVaultKeyCollection key): base(key) { }

        protected override string GetTargetProcessLocation()
        {
            string SystemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!Directory.Exists(SystemDir))
            {
                throw new InvalidOperationException("Someone the system folder is not existing?");
            }
            if (SystemDir.EndsWith(Path.DirectorySeparatorChar))
                SystemDir = SystemDir.TrimEnd(Path.DirectorySeparatorChar);
            if (SystemDir.EndsWith(Path.AltDirectorySeparatorChar))
                SystemDir = SystemDir.TrimEnd(Path.AltDirectorySeparatorChar);

            return SystemDir +  Path.DirectorySeparatorChar + "ping.exe";
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
