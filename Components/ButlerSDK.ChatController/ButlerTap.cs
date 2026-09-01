using ButlerSDK.ToolSupport;
using ButlerToolContract.DataTypes;
using System.Text;
using System.Text.Json;

namespace ButlerSDK.Debugging
{
    /// <summary>
    /// This is for debugging. Each time <see cref="ButlerSDK.Butler.StreamResponse(ButlerSDK.Butler.ChatMessageStreamHandler, bool)"/> mutates its list. It dumps it here
    /// </summary>
    public class ButlerTap :  IDisposable
    {
        protected Stream Target;
        private bool disposedValue;
        static JsonSerializerOptions Opts = new ()
            {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
            WriteIndented = true
            };
        static JsonSerializerOptions JustIndentIt = new JsonSerializerOptions() { WriteIndented = true };
        public ButlerTap(Stream Target)
        {
             this.Target = Target;

        }

        public void WriteString(string Value, bool AtEnd=true)
        {

        }
        public static void WriteString(Stream Target, string Value, bool AtEnd=true)
        {

            var data = Encoding.UTF8.GetBytes(Value);
            if (AtEnd)
            {
                Target.Seek(0, SeekOrigin.End);
            }
            Target.Write(data);
            Target.Flush();
        }
        public void BeginLog()
        {
   
             WriteString(this.Target, @$"---  Beginning Tap ----  {DateTimeOffset.Now.ToString("o")} \r\n");
           
        }
        /*
        public void LogTelemetryStats(ToolResolverTelemetryStats? Stats)
        {
            if (Stats is not null)
            {
                var str = JsonSerializer.Serialize<ToolResolverTelemetryStats>(Stats, JustIndentIt);
                WriteString(this.Target, str);
            }
        }*/

        public void LogString(string msg)
        {
            WriteString(this.Target, msg);
        }
        public void LogStreamingUpdate( ButlerStreamingChatCompletionUpdate Update)
        {
            string? str;
            if (Update.IsEmpty())
            {
                return;
            }
            str = JsonSerializer.Serialize<ButlerStreamingChatCompletionUpdate>(Update, new JsonSerializerOptions() { WriteIndented = true });
            WriteString(this.Target, str);
        }
        public void LogStreamingPart(ButlerChatStreamingPart Part)
        {
            string? str;
            str = JsonSerializer.Serialize<ButlerChatStreamingPart>(Part, new JsonSerializerOptions() { WriteIndented = true });

            WriteString(this.Target, str);

        }
        public void Dump(IList<ButlerChatMessage> list)
        {
         
            
                int line = 1;
                string? str = "---- Dump OF MESSAGES at " + DateTimeOffset.Now.ToString("o") + " ----\r\n";
                WriteString(this.Target, str);
                foreach (var item in list)
                {


                    str = JsonSerializer.Serialize<ButlerChatMessage> (item, new JsonSerializerOptions() { WriteIndented = true });
                    if (str is not null)
                    {
                        WriteString(this.Target,$"--- Message {line++} ----\r\n");
                        WriteString(this.Target, str);
                    }

                }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                
                if (disposing)
                {
                    this.Target.Flush();
                    this.Target.Dispose();
                    this.Target = null!;// ! removing our reference is fine
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

       
    }
}
