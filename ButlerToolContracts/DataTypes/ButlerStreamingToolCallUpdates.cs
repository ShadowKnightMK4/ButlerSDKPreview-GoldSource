using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public class ButlerStreamingToolCallUpdatePart
    {
        public ButlerStreamingToolCallUpdatePart(string? Name, string? arguments, int Index, string Kind, string? CallID)
        {
            this.FunctionName = Name!; // we can't promise it's not null. BUT I want the warnings to trigger if sometime is trying to use this without ensuring something is not null if working with this data type
            this.FunctionArgumentsUpdate = arguments;
            this.Index =   Index;
            this.Kind = Kind;
            this.ToolCallid = CallID!;  // we can't promise it's not null. BUT I want the warnings to trigger if sometime is trying to use this without ensuring something is not null if working with this data type
        }
 
        /// <summary>
        /// Function arguments for the function called, can be null (no args)
        /// </summary>
        public string? FunctionArgumentsUpdate;
        /// <summary>
        /// name of the function called. You must set this.
        /// </summary>
        public string FunctionName;
        public int Index;
        public string Kind;
        public string ToolCallid;
    }
}
