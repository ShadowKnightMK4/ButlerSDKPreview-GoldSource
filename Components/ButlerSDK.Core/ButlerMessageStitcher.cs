using ButlerToolContract.DataTypes;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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

            if (Part.ContentUpdate is not null)
            {
                for (int i =0; i< Part.ContentUpdate.Count; i++)
                {
                    ButlerChatMessageContentPart SubPart = new();
                    SubPart.Text = Part.ContentUpdate[i].Text;
                    SubPart.MessageType = Part.ContentUpdate[i].MessageType;
                    // dear future: this code is needing changing once supporting models that do non text
                    SubPart.MessageType = ButlerChatMessageType.Text;
                    foreach (string key in Part.ContentUpdate[i].ProviderSpecfic.Keys)
                    {
                        SubPart.ProviderSpecific[key] = Part.ContentUpdate[i].ProviderSpecfic[key];
                    }

                  
  

                    if (!string.IsNullOrEmpty(SubPart.Text))
                    {
                        if (BuildingMessage.Content.Count != 0)
                        {
                           BuildingMessage.Content[i].Text += SubPart.Text;
                        }
                        else
                        {
                            BuildingMessage.Content.Add(SubPart);
             
                        }
                    }

                    if (SubPart.MessageType != ButlerChatMessageType.Text)
                    {
                        switch (SubPart.MessageType)
                        {
                            case ButlerChatMessageType.Refusal:
                                {
                                    Part.ContentUpdate[i].MessageType = ButlerChatMessageType.Refusal;
                                    break;
                                }
                            case ButlerChatMessageType.Unknown:
                            case ButlerChatMessageType.File:
                            case ButlerChatMessageType.Image:
                            case ButlerChatMessageType.Audio:
                                throw new NotImplementedException("Unsupported Gemini part type. While Gemini models support images/audio ect..., butler currently does not supprt any part type except text");
                            default:
                                throw new NotImplementedException("Unspecified message type");

                        }
                        
                    }
                  
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
