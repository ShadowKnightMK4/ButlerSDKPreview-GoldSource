using ButlerSDK.Debugging;
using ButlerSDK.ToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ButlerSDK
{
    public static class bt_ext
    {
        static JsonSerializerOptions JustIndentIt = new JsonSerializerOptions() { WriteIndented = true };
        public static void LogTelemetryStats(this ButlerTap T, ToolResolverTelemetryStats? Stats)
        {
            if (Stats is not null)
            {
                var str = JsonSerializer.Serialize<ToolResolverTelemetryStats>(Stats, JustIndentIt);
                T.WriteString(str, true);
            }
        }
    }
}
