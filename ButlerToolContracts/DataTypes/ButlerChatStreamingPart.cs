using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    
    public enum ButlerChatFinishReason
    {
        /// <summary>
        /// The model stopped generating
        /// </summary>
        Stop =0,
        /// <summary>
        /// The model hit the max length of tokens it could generate (either by user setting or model limit)
        /// </summary>
        Length = 1,
        /// <summary>
        /// A model tripped a content filter such as image safety , block list, or other safety system
        /// </summary>
        ContentFilter = 3,
        /// <summary>
        /// The model made a tool call
        /// </summary>
        ToolCalls = 4,
        /// <summary>
        /// For complention sake: Openai depreicited function calls. And the other providers (Gemini, Openai, Ollama) use tool calls instead
        /// </summary>
        FunctionCall = 5,
        /// <summary>
        /// The model had an error generating output for a reason that is NOT related to content filtering
        /// </summary>
        TechnicalError = 6
    }

    public enum ButlerChatMessagePartKind
    {
        Unspecified = 0,
        Text = 1,
        Refusal = 2,
        Image = 3

    }
    
    public static class ButlerChatMessagePartKindConverter
    {
        public static ButlerChatMessagePartKind Convert(ButlerChatMessageType ThisOne)
        {
            switch (ThisOne)
            {
                case ButlerChatMessageType.Text:
                    return ButlerChatMessagePartKind.Text;
                case ButlerChatMessageType.Image:
                    return ButlerChatMessagePartKind.Image;
                case ButlerChatMessageType.Refusal:
                    return ButlerChatMessagePartKind.Refusal;
                default:
                    return ButlerChatMessagePartKind.Unspecified;
            }
        }

        public static ButlerChatMessageType Convert(ButlerChatMessagePartKind ThisOne)
        {
            switch (ThisOne)
            {
                case ButlerChatMessagePartKind.Text:
                    return ButlerChatMessageType.Text;
                case ButlerChatMessagePartKind.Image:
                    return ButlerChatMessageType.Image;
                case ButlerChatMessagePartKind.Refusal:
                    return ButlerChatMessageType.Refusal;
                default:
                    return ButlerChatMessageType.Unknown;
            }
        }
    }
    public class ButlerChatStreamingPart
    {
  
        public ButlerChatMessagePartKind Kind;
        public ButlerChatFinishReason? FinishReason;
        public ButlerChatMessageType MessageType;
        public string? Text;
        public Dictionary<string, string> ProviderSpecfic=new();
    }

}
