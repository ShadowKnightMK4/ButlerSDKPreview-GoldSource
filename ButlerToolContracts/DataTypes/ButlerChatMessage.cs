using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public enum ButlerChatMessageRole
    {
        None = 0,
        Assistant = 1,
        /// <summary>
        /// User made message
        /// </summary>
        User = 2,
        /// <summary>
        /// A request by the LLM for a tool
        /// </summary>
        ToolCall = 3,
        /// <summary>
        /// The results of a tool request
        /// </summary>
        ToolResult = 4,
        /// <summary>
        /// System level message (usual willy trump user message for guiding)
        /// </summary>
        System =5,
    }

    public enum ButlerChatMessageType
    {
        Unknown = 0,
        Text = 1,
        Image = 2,
        Refusal = 3,
        /// <summary>
        /// NOT SUPPORTED
        /// </summary>
        Audio = 4,
        /// <summary>
        /// NOT SUPPORTED 
        /// </summary>
        File =5

    }

    /// <summary>
    /// ButlerChatMessage and its flavors are the generic wrapper class for messages. An LLM provide must be able to take the message and convert back and forth as needed to its underyling platform
    /// </summary>
    public class ButlerChatMessage
    {
        public ButlerChatMessageRole Role { get; set; }
        
        public string? Message { get; set; }

        public string? Id { get; set; }

        public string? Participant { get; set; }
        public ButlerChatMessage(string Message)
        {
            ButlerChatMessageContentPart part = new ButlerChatMessageContentPart();
            part.Text = Message;
            part.Id = Id;
            part.MessageType = ButlerChatMessageType.Text;
            part.Role = ButlerChatMessageRole.None;

            this.Content.Add(part);
        }

 
        /// <summary>
        /// Create a blank null message. will need to fill it out
        /// </summary>
        public ButlerChatMessage() 
        {
            
        }

        /// <summary>
        /// When true, the message is gonna be culled by butler when the ai turn ends. Chats are also not stored in history 'audit log'
        /// </summary>
        public bool IsTemporary
        {
            get; set; 
        }
        public List<ButlerChatMessageContentPart> Content { get; set; } = new();


        /// <summary>
        /// Loop thu the content parts and combine all text parts into a single string
        /// </summary>
        /// <returns></returns>
        public string GetCombinedText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in Content)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    sb.Append(part.Text);
                }
            }
            return sb.ToString();
        }

    }

    /// <summary>
    /// Create a generic chat message preseeded for assistant (llm output) role
    /// </summary>
    public class ButlerAssistantChatMessage : ButlerChatMessage
    {
        public ButlerAssistantChatMessage(string Message): base(Message)
        {
            Role = ButlerChatMessageRole.Assistant;
        }
    }

    /// <summary>
    /// Create a generic chat message preceeded for user text (data sent to llm) role
    /// </summary>
    public class ButlerUserChatMessage : ButlerChatMessage
    {
        public ButlerUserChatMessage(string Message) : base(Message)
        {
            Role = ButlerChatMessageRole.User;
        }
    }

    /// <summary>
    /// Create a generic tool call message  (results of invoking a tool) (data generated from something llm requests)
    /// </summary>

    public class ButlerChatToolResultMessage : ButlerChatToolCallMessage
    {
        public ButlerChatToolResultMessage(string? callID, string result) : base(callID, result)
        {
            Role = ButlerChatMessageRole.ToolResult;
            this.Id = callID;
            this.Message = result;
            this.ToolName = string.Empty; 
            this.FunctionArguments = null;

        }

        public ButlerChatToolResultMessage(string? CallID, string ToolName, string? Args) : base(CallID, ToolName, Args)
        {
            Role = ButlerChatMessageRole.ToolResult;
            this.ToolName = ToolName;
            this.FunctionArguments = null;
            this.Message = null;
        }

        public static ButlerChatToolResultMessage CreateFunctionToolResult(string CallID, string ToolName, string Args, string result)
        {
            var ret = new ButlerChatToolResultMessage(CallID, result);
            ret.ToolName = ToolName;
   
            ret.Message = result;
            ret.FunctionArguments = Args;
            return ret;
        }
    }
    public class ButlerChatToolCallMessage : ButlerChatMessage
    {

        public ButlerChatToolCallMessage(string? callID, string RequestSpecs): base(RequestSpecs)
        {
            Role = ButlerChatMessageRole.ToolCall;
            this.Id = callID;
            this.Message = RequestSpecs;
            this.ToolName = string.Empty;
            this.FunctionArguments = string.Empty;
            this.Role = ButlerChatMessageRole.ToolCall;
        }

        public ButlerChatToolCallMessage(string? CallID, string ToolName, string? Args) : base(string.Empty)
        {
            Role = ButlerChatMessageRole.ToolCall;
            this.Id = CallID;
            this.ToolName = ToolName;
            this.FunctionArguments = Args;
//#warning temp question fix. Dropping content part for Gemini test
// update: seems to work with the existing code. leaving it for now
            base.Content.Clear();
        }

        public string ToolName { get; set; }
        public string? FunctionArguments { get; set; }

        public static ButlerChatToolCallMessage CreateFunctionToolCall(string ID, string ToolName, string ToolArgs )
        {
            var ret = new ButlerChatToolCallMessage(ID, ToolName, ToolArgs);
            return ret;
        }
    }

    /// <summary>
    /// Creat ea generic system char message (higher priority than user, typically will providing llm starting directions)
    /// </summary>
    public class ButlerSystemChatMessage : ButlerChatMessage
    {
        public ButlerSystemChatMessage(string Message) : base(Message)
        {
            Role = ButlerChatMessageRole.System;
        }
    }


}
