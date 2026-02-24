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
    /// <summary>
    /// A virtual tool in ButlerSDK context is a tool that houses other tools. Implementing <see cref="IButlerToolContainsPrivateTools"/> which VirtualTool does lets you chain instances of other tools as you want in a single call from the LLM.
    /// </summary>
    /// <remarks>Once current example of VirtualTool is <see href="https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource/blob/master/NonExtNetworkTools/ButlerTool_DeviceApi_GotNetwork.cs"/> which chains Adapter Check, DnsLookup and Ping (other tools) in a single call</remarks>
    [ButlerTool_DiscoverAttributes(true)]
    public abstract class VirtualTool : IButlerToolBaseInterface, IButlerToolContainsPrivateTools
    {
        IButlerVaultKeyCollection? _vaultKeyCollection=null;
        public VirtualTool(IButlerVaultKeyCollection Keys)
        {
            _vaultKeyCollection = Keys;
        }
        /// <summary>
        /// the inner tools that make up this virtual tool. By default this is not public. No Harm in making it public as child class
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
