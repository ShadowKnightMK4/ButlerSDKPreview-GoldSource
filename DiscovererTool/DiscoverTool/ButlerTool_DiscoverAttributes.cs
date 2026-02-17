using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.ToolSupport.DiscoverTool
{
    [AttributeUsage(AttributeTargets.Class, Inherited =true)]
    
    /// <summary>
    /// Classes flagged with this by default DO NOT get swept into the default discover tool cache. Usefule for risky tools
    /// </summary>
    public class ButlerTool_DiscoverAttributes: Attribute
    {
        public ButlerTool_DiscoverAttributes(bool DisableDiscovery)
        {
            this.DisableDiscover = DisableDiscovery;
        }
        /// <summary>
        /// if true, by default Tool discover WILL NOT add this tool.
        /// </summary>
        public bool DisableDiscover {  get; set; }
    }
}
