using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public  class ButlerChatMessageContentPart
    {
        public ButlerChatMessageRole Role { get; set; }
        public ButlerChatMessageType MessageType { get; set; }

        public string? Refusal { get; set; }
        public string? Text { get; set; }

        public string? Id { get; set; }

        public Dictionary<string, string> ProviderSpecific = new();
 
    }
}
