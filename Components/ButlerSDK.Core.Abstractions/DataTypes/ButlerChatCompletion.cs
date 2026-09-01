using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public class ButlerChatCompletion
    {
        
        /// <summary>
        /// Butler will use this to  access tool calls
        /// </summary>
        public IReadOnlyList<ButlerChatToolCallMessage> ToolCalls { get => (IReadOnlyList<ButlerChatToolCallMessage>)ToolCallsList; }



        private List<ButlerChatToolCallMessage> ToolCallsList = new List<ButlerChatToolCallMessage>();
    }
}
