using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerToolContract;
using ButlerToolContract.DataTypes;
using ButlerSDK.ToolSupport.DiscoverTool;
using ButlerSDK.ApiKeyMgr.Contract;
namespace ButlerSDK.ToolSupport
{
    [ButlerTool_DiscoverAttributes(true)]
    public abstract class VirtualTool : IButlerToolBaseInterface, IButlerToolContainsPrivateTools
    {
        IButlerVaultKeyCollection? _vaultKeyCollection=null;
        public VirtualTool(IButlerVaultKeyCollection Keys)
        {
            _vaultKeyCollection = Keys;
        }
        /// <summary>
        /// the inner tools that make up this virtual tool. By default this is not public. No Harm in making it butler as child class
        /// </summary>
        protected readonly Dictionary<string, IButlerToolBaseInterface> _InnerTools = new();

        public virtual string ToolName => throw new NotImplementedException();

        public virtual string ToolVersion => throw new NotImplementedException();

        public virtual string ToolDescription => throw new NotImplementedException();

        public virtual string GetToolJsonString()
        {
            throw new NotImplementedException();
        }

        public abstract ButlerChatToolResultMessage? ResolveMyTool(string? FunctionCallArguments, string? FuncId, ButlerChatToolCallMessage? Call);

        public virtual bool ValidateToolArgs(ButlerChatToolCallMessage? Call, JsonDocument? FunctionParse)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, IButlerToolBaseInterface> GetInterfaces()
        {
            return this._InnerTools;
        }
    }
}
