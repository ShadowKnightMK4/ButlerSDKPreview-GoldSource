using ButlerToolContract.DataTypes;
using System.Text;

namespace ButlerSDK
{
    /// <summary>
    /// This class takes the bits and bobs of x as streamed to <see cref="Butler"/> and assembles message from that.
    /// </summary>
    /// <remarks>This is an internal class and can potentially change from release to release. Use with care.</remarks>
    internal class ButlerMessageStitcher
    {
        ButlerChatMessage BuildingMessage = new ButlerChatMessage();


        
        
     

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

        public void Append(ButlerStreamingChatCompletionUpdate Part)
        {
            if (Part.FunctionArgumentsUpdate is not null)
            {
                // nothing
            }
            
            



            
            

            if (! string.IsNullOrWhiteSpace(Part.Id))
            {
                if (BuildingMessage.Id is not null)
                {
                    BuildingMessage.Id = Part.Id;
                }
                else
                {
                    if (!string.IsNullOrEmpty(Part.Id))
                    {
                        BuildingMessage.Id += Part.Id;
                    }
                }
            }


            if (! string.IsNullOrWhiteSpace(Part.Model))
            {
                //BuildingMessage.Model = Part.Model;
            }

            foreach (string key in Part.ProviderSpecificComponents.Keys)
            {
                BuildingMessage.ProviderSpecific[key] =Part.ProviderSpecificComponents[key];
            }

            if ( !string.IsNullOrWhiteSpace(Part.RefusalUpdate))
            {
                
            }

            if (Part.Role is not null)
            {
                BuildingMessage.Role = (ButlerChatMessageRole)Part.Role;
            }

            if (Part.ToolCallUpdates is not null)
            {
                for (int i = 0; i < Part.ToolCallUpdates.Count; i++)
                {
                    var NewMsg = ButlerChatToolCallMessage.CreateFunctionToolCall(Part.ToolCallUpdates[i].ToolCallid, Part.ToolCallUpdates[i].FunctionName, Part.ToolCallUpdates[i].FunctionArgumentsUpdate);

                    NewMsg.Role = BuildingMessage.Role;
                    NewMsg.Message = BuildingMessage.Message;
                    foreach (var part in BuildingMessage.ProviderSpecific.Keys)
                    {
                        NewMsg.ProviderSpecific[part] = BuildingMessage.ProviderSpecific[part];
                    }
                    foreach (var part in Part.ToolCallUpdates[i].ProviderSpecific.Keys)
                    {
                        NewMsg.ProviderSpecific[part] = Part.ToolCallUpdates[i].ProviderSpecific[part];
                    }
                    BuildingMessage = NewMsg;
                    

                }
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
