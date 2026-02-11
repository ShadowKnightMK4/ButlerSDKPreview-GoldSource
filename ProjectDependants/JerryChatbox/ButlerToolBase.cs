using ButlerToolContract;
using ButlerToolContract.DataTypes;
using System.Text.Json;
using ButlerSDK.ApiKeyMgr.Contract;


namespace ButlerSDK
{ 
    public class ApiKeyNotFound: Exception
    {
        public ApiKeyNotFound():base() { }
        public ApiKeyNotFound(string message) : base(message) { }
        public ApiKeyNotFound(string message, Exception innerException) : base(message, innerException) { }

        internal static string CannedMessage(string v)
        {
            return $"The Butler tool {v} uses keys and you must pass a class that defines the {nameof(IButlerVaultKeyCollection)} interface";
        }
    }
    /// <summary>
    /// ButlerSDK's tools are instances of this class
    /// </summary>
    public abstract class ButlerToolBase: IButlerToolBaseInterface, IButlerToolSpinup, IButlerToolWindDown, IButlerToolPlatformPass, IButlerToolPostCallInjection
    {
        /// <summary>
        /// For when your tool requires no arguments, no parameters and that's it.
        /// </summary>
         public static readonly string NoArgJson = @"{
    ""type"": ""object"",
    ""properties"": {
    },
    ""required"": [ ]
}";
        #region common helpers
        /// <summary>
        /// will build a json format to return to openai llm
        /// </summary>
        /// <param name="FuncId"> You should in your <see cref="ResolveMyTool(string, string, ChatToolCall?)"/> code pass the FuncID string to this </param>
        /// <param name="status">Should be a status code or meaningful value</param>
        /// <param name="content">content to include</param>
        /// <returns>tool char message</returns>
        /*protected ToolChatMessage BuildReturnVal(string FuncId, string status, string content)
        {
            Dictionary<string, string> json = new();
            json["CODE"] = status;
            json["CONTENT"] = content;
            return new ToolChatMessage(FuncId, JsonSerializer.Serialize(json));
        }*/
        #endregion
        /// <summary>
        /// May be null. Is the app's key store
        /// </summary>
        protected IButlerVaultKeyCollection? Handler = null;

        /// <summary>
        /// Make an instance of this tool with the supplied key handler. The tool may possibly not needed it.
        /// </summary>
        /// <param name="KeyHandler"></param>
        /// <remarks>Should your tool be unable to get the API key it needs, throw an <see cref="ApiKeyNotFound"/> for the <see cref="Butler"/></remarks>
        public ButlerToolBase(IButlerVaultKeyCollection? KeyHandler)
        {
            Handler = KeyHandler;
        }
       
        
        public abstract string ToolName { get; }

        public abstract string ToolDescription { get; }
        
        public abstract string ToolVersion { get; }


        public abstract bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse);
        /// <summary>
        /// Extract Function arguments and call id - then hands it off to <see cref="ResolveMyTool(string, string, ButlerChatToolCallMessage?)"/> with the third arg as null
        /// </summary>
        /// <param name="Call"><see cref="ChatToolCall"/> message supplied by the OpenAI .net sdk or equivalent </param>
        /// <returns>Depending on the inner call could return null on failure, otherwise a resolved tool message</returns>
        public virtual ButlerChatToolResultMessage? ResolveMyTool(ButlerChatToolCallMessage Call)
        {
            ArgumentNullException.ThrowIfNull(Call);
            string args;
            if (Call.FunctionArguments is null)
            {
                args = JsonSerializer.Serialize(NoArgJson);
            }
            else
            {
                args = Call.FunctionArguments;
            }
                return ResolveMyTool(args, Call.Id, null);
        }

        /// <summary>
        /// Bit of assist boiler plat code. Meant to be called by any <see cref="ResolveMyTool(string, string, ChatToolCall?)"/>.  Validates if we have non null args, extracts from either FunctionCall or Call with preference to use Call if not null. Sends the json document passed to it to args and will return true if OK. DOES NOT VALIDATE. Just gets ball rolling.
        /// </summary>
        /// <param name="FunctionCallArguments">the same as <see cref="ResolveMyTool(string, string, ButlerChatToolCallMessage?)"/> </param>
        /// <param name="FuncId">the same as <see cref="ResolveMyTool(string, string, ButlerChatToolCallMessage?)</param>
        /// <param name="Call">the same as <see cref="ResolveMyTool(string, string, ButlerChatToolCallMessage?)</param>
        /// <param name="that">This should the this pointer of the tool using this code.</param>
        /// <param name="args">The results of extracting the args for the tool call</param>
        /// <returns>true if the thing passed validation or false if not</returns>
        /// <exception cref="ArgumentException">triggers if everything is null </exception>
        public static bool BoilerPlateToolResolve(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call, IButlerToolBaseInterface that, out JsonDocument args)
        {
            
            if (string.IsNullOrEmpty(FunctionCallArguments) && string.IsNullOrEmpty(FuncId) && (Call is null))
            {
                throw new ArgumentException("Pick either using FunctioncCallArgs as json string + func id, OR Call must NOT BE null");
            }
            if (Call is not null)
            {
                if (Call.FunctionArguments is not null)
                {
                    args = JsonDocument.Parse(Call.FunctionArguments);
                }
                else
                {
                    args = JsonDocument.Parse(NoArgJson);
                }
                if (that.ValidateToolArgs(Call, args) == false)
                    return false;
                return true;
            }
            else
            {
                if (string.IsNullOrEmpty(FunctionCallArguments))
                {
                    args = JsonDocument.Parse(NoArgJson);
                }
                else
                    args = JsonDocument.Parse(FunctionCallArguments);

                if (that.ValidateToolArgs(null, args) == false)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// resolve your tool.  Your returned ToolChatMessage will be added to the list.
        /// </summary>
        /// <param name="FunctionCallArguments">Should expect (but very it's a JsonDoc). If you're comparing OpenAI .NET SDK - see ChatToolCall.FunctionParameters. This is the arguments the LLM sent your tool </param>
        /// <param name="FuncId">will be a string. Could be null (and you should plan for that!. Just place it in ButlerChatToolResult.CallId. If you're coming from OpenAI .NET SDK <see cref="ChatToolCall.Id"/></param>
        
        /// <param name="Call">Default is gonna be null. Override <see cref="ResolveMyTool(ButlerChatToolCallMessage)"/> to call this and pass the object.</param>
        /// <returns>you return null if you can't complete do the action. Not having a resource is perfectly fine BUT if your tool can't due to errors return null.</returns>
        /// <remarks>Write your code to handle to tool call here and NOT <see cref="ResolveMyTool(ChatToolCall)"/>.  Play null defense the Call argument and strings.  Given a choice, use the strings first then callback to Call if you cant </remarks>.
        /// <exception cref="ArgumentException">Should be thrown if not of the arguments are valid.</exception>
        public abstract ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call);

        

        /// <summary>
        /// Get the json template to feed to the LLM Model (ChatGpt/ Gemini/ Local models). Most of the prebuilt tools just use a hard-coded string and feed to JsonDoc
        /// </summary>
        /// <returns></returns>
        public virtual JsonDocument GetToolJsonTemplate()
        {
            return JsonDocument.Parse(GetToolJsonString());
        }


        /// <summary>
        /// override this to return your JSON template that is fed to the OpenAi LLM
        /// </summary>
        /// <returns></returns>
        public abstract string GetToolJsonString();
        
        
        


        #region event system as virtual functions
            /// <summary>
            /// This is called whenever the LLM class Butler2 will add a message to its list potentially knocked off an older one
            /// </summary>
            /// <param name="message"></param>
        public virtual void OnMessageAdded(ButlerChatMessage message)
        {

        }

        public virtual void Initialize()
        {
            
        }

        public virtual void WindDown()
        {
            
        }



        /// <summary>
        /// Message by default is <see cref="String.Empty"/>
        /// </summary>
        /// <param name="message">set your message to the LLM post call. Might not actually be sent. return true if running on something you support (i.e. you need ping.exe on Windows and .NET is windows vs say Linux</param>
        /// <returns></returns>
        /// <remarks>This code is called *before* <see cref="Initialize"/> return true to proceed </remarks>
        public virtual bool CheckPlatformNeed(out string message)
        {
            message = string.Empty;
            return true;
        }

        public virtual string GetToolPostCallDirection()
        {
            return "A tool/function was called. USE THE OUTPUT!";
        }
        #endregion
    }
}
