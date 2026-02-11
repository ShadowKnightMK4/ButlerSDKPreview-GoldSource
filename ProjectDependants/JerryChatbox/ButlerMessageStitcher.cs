using ButlerToolContract.DataTypes;

namespace ButlerSDK
{
    /// <summary>
    /// This class takes the bits and bobs of x as streamed to <see cref="Butler"/> and assembles message from that.
    /// </summary>
    /// <remarks>This is an internal class and can potentially change from release to release. Use with care.</remarks>
    internal class ButlerMessageStitcher
    {
        ButlerChatMessage BuildingMessage = new ButlerChatMessage();


        
        
        public void Append(ButlerChatStreamingPart Part)
        {
            bool lifted_last_message = false;
            ButlerChatMessageContentPart? last = null;
            if (BuildingMessage.Content.Count > 0)
            {
                last = BuildingMessage.Content[BuildingMessage.Content.Count-1];
                lifted_last_message = true;
            }
            else
            {
                last = null;
            }

            if (last == null)
            {
                last = new ButlerChatMessageContentPart();
            }
            else
            {

                // this logic filters if we're getting diff match
                if (last?.MessageType !=  ButlerChatMessagePartKindConverter.Convert(Part.Kind))
                {
                    last = null;
                }
                
            }

                // next compare
           if (last == null)
            {
                last = new();
            }
           // dear future me: when you add video and more, you'll need to account for more than
           // one data type here.
            if (last.MessageType == ButlerChatMessageType.Unknown)
            {
                last.MessageType = Part.MessageType;
            }

            if (last.MessageType == ButlerChatMessageType.Unknown)
            {
                if (Part.Text is not null)
                {
                    last.MessageType = ButlerChatMessageType.Text;
                }
            }
   
            last.Text += Part.Text;

            

            if (!lifted_last_message)
                BuildingMessage.Content.Add(last);
        }

        static bool HasContent(ButlerStreamingToolCallUpdatePart Part)
        {
            if (Part == null) return false;
            if (Part.FunctionName != null) return true;
            if (Part.FunctionArgumentsUpdate != null)   return true;
            if (Part.Kind != null) return true;
            if (Part.ToolCallid  != null) return true;
            return false;
        }
        static bool HasContent(ButlerChatStreamingPart part)
        {
            if (part == null) return false;
            if (part.FinishReason != null)
            {
                return true;
            }
            if (part.MessageType != 0) return true;
            if (string.IsNullOrEmpty(part.Text) == false) return true;
            if (part.ProviderSpecfic.Count is not 0) return true;
            return false;
        }
        public void Append(ButlerStreamingChatCompletionUpdate Part )
       {

            if (Part.ToolCallUpdates.Count != 0)
            {

                var ReplacementGoldish = ButlerChatToolCallMessage.CreateFunctionToolCall(
                    Part.ToolCallUpdates[0].ToolCallid,
                    Part.ToolCallUpdates[0].FunctionName,
                    Part.ToolCallUpdates[0].FunctionArgumentsUpdate!); // ! reason: Null or not, it's fine. We're passing as it
                foreach (var content in BuildingMessage.Content)
                {
                    ReplacementGoldish.Content.Add(content);
                }
                ReplacementGoldish.Message = BuildingMessage.Message;
                this.BuildingMessage = ReplacementGoldish;
                return;
            }
            else
            {
                for (int i = 0; i < Part.ContentUpdate.Count; i++)
                {
                    if (HasContent(Part.ContentUpdate[i]))
                    {
                        Append(Part.ContentUpdate[i]);
                        
                    }
                }
                if (Part.Role is not null)
                    this.BuildingMessage.Role = (ButlerChatMessageRole) Part.Role;
            }
        }
        public ButlerChatMessage GetMessage(ButlerChatMessageRole? ForceRoll=null)
        {
            
            if (ForceRoll is not null)
            {
                
                this.BuildingMessage.Role = (ButlerChatMessageRole)ForceRoll;
                for (int i =0 ; i < this.BuildingMessage.Content.Count; i++)
                {
                    this.BuildingMessage.Content[i].Role = (ButlerChatMessageRole) ForceRoll;
                }
            }
            
            
            return BuildingMessage; 
        }
    }
}
