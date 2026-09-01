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
    public class ButlerTool_DeviceAPI_ExternProcessIpConfig_FlushDNS : ButlerTool_DeviceAPI_ExternProcessBase
    {
        public override string ToolName => "FlushDNS";

        public override string ToolDescription => "Calls IPCONFIG /flushdns equivalent on running device";

        public override string ToolVersion => "YES";

        public override string GetToolJsonString()
        {
            return NoArgJson;
        }
        public ButlerTool_DeviceAPI_ExternProcessIpConfig_FlushDNS(IButlerVaultKeyCollection key): base(key) { }

        protected override string GetTargetProcessLocation()
        {
            string sysdir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (!Directory.Exists(sysdir))
            {
                throw new InvalidOperationException("Someone the system folder is not existing?");
            }
            if (sysdir.EndsWith(Path.DirectorySeparatorChar))
                sysdir = sysdir.TrimEnd(Path.DirectorySeparatorChar);
            if (sysdir.EndsWith(Path.AltDirectorySeparatorChar))
                sysdir = sysdir.TrimEnd(Path.AltDirectorySeparatorChar);

            return sysdir + Path.DirectorySeparatorChar + "ipconfig.exe";
        }

        protected override string BuildProcessArgument(string? FunctionCallArgs)
        {
            return "/flushdns";
        }



      
    }
}
