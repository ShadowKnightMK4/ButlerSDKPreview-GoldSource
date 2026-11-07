using ButlerSDK;
using ButlerSDK.ApiKeyMgr;
using ButlerSDK.Providers.Gemini.Vertex;
using ButlerSDK.Tools;
using ButlerSDK.ToolSupport;
using ButlerToolContract.DataTypes;

/// <summary>
/// Example of using vertex 
/// </summary>
static class Program
{    /// <summary>
     /// the handler delegate is used to expose a bit of the stream to dev (depending on the Counter Measures object (which is optional)
     /// </summary>
     /// <param name="content">the most recent packet</param>
     /// <param name="msg">the collection of message the stream is currently handling</param>
     /// <returns>You should return true to keep going</returns>
     /// <remarks>Keep in mind the stream is frozen until you continue</remarks>
    static bool SimpleHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
    {
        // here we just loop thru the content, 
        for (int i = 0; i < content.ContentUpdate.Count; i++)
        {
            if (!string.IsNullOrEmpty(content.ContentUpdate[i].Text))
                Console.WriteLine(content.ContentUpdate[i].Text);
        }
        return true;
    }
    static async Task<int> Main(string[] args)
    {
        VertexGeminiProvider VertexProvider = new VertexGeminiProvider();

        var ToolTime = new ButlerTool_DeviceAPI_GetLocalDateTime(null);
        return 0;

    }


}