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
    public class ButlerTool_DeviceAPI_ExternProcessNetworkAdaptor : ButlerTool_DeviceAPI_ExternProcessBase
    {
        public override string ToolName => "NetworkAdaptorInfo";

        public override string ToolDescription => "Calls IPCONFIG.EXE /all on equal on the running device";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return NoArgJson;
        }
        public ButlerTool_DeviceAPI_ExternProcessNetworkAdaptor(IButlerVaultKeyCollection key): base(key) { }

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

            return SystemDir + Path.DirectorySeparatorChar + "ipconfig.exe";
        }

        protected override string BuildProcessArgument(string? FunctionCallArgs)
        {
            return "/all";
        }



      
    }
}
