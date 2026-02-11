using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.ToolSupport;
using ButlerToolContract;

namespace ButlerSDK
{
    /// <summary>
    /// A simple implementation of the pairing mechanic for system tools and butler
    /// </summary>
    public abstract class ButlerSystemToolBase : ButlerToolBase, IButlerSystemToolInterface
    {
        /// <summary>
        /// The paired tool kit your system tool should work with, check for null please
        /// </summary>
        protected ButlerToolBench? MyTools;
        /// <summary>
        /// The paired butler your tool should work with, check for null please
        /// </summary>
        protected IButlerChatSession? Jeeves;
        public ButlerSystemToolBase(IButlerVaultKeyCollection? KeyHandler) : base(KeyHandler)
        {

        }

        /// <summary>
        /// Pair this system tool with the specific butler and <see cref="ButlerToolBench"/>
        /// </summary>
        /// <param name="UseMe"></param>
        /// <param name="ToolKit"></param>
        public void Pair(IButlerChatSession? UseMe, object? ToolKit)
        {
            if (UseMe is not null)
                Jeeves = UseMe;
            if ((ToolKit is not null) && (ToolKit is ButlerToolBench))
            {
                MyTools = (ButlerToolBench)ToolKit;
            }
        }

        public void UnpairButler()
        {
            Jeeves = null;
        }

        public void UnpairToolKit()
        {
            MyTools = null;
        }
    }
}
