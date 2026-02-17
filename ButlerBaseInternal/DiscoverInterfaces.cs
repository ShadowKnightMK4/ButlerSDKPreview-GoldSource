using ButlerSDK.ApiKeyMgr.Contract;
using ButlerSDK.Core;
using ButlerSDK.ToolSupport.Bench;
using ButlerToolContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerBaseInternal
{
    public interface ButlerTool_DiscoverResource
    {
        /// <summary>
        /// Initialize this resource. Your implementation should use the passed argument for any API key needs
        /// </summary>
        /// <param name="KeyHandler">The implementation should use this to inject KeyHandler into the tools it spins up</param>
        /// <remarks>Your code should pick to either pass this to the tools unchanged or swap a non null one on getting null. Default <see cref="ButlerTool_DiscoverTools"/> passes it thru as is. Others might not.</remarks>
        public void Initialize(IButlerVaultKeyCollection? KeyHandler);

        /// <summary>
        /// Search for a list of matching tools (returns names) that match the single term sent
        /// </summary>
        /// <param name="term">term to search for </param>
        /// <returns>returns a list of tool names that match the term searched for</returns>
        public IList<string> Search(string term);

        /// <summary>
        /// Search for a list of matching tools (returns names) that match this list of terms to look for
        /// </summary>
        /// <param name="terms"></param>
        /// <returns>returns a list of tool names that match the term searched for</returns>
        public IList<string> Search(string[] terms);

        /// <summary>
        /// Spawn this collection of tools and return a list of them.
        /// </summary>
        /// <param name="Names">names of </param>
        /// <returns></returns>
        /// <remarks>Likely quickest way to get is <see cref="Search(string[])"/> or <see cref="Search(string)"/></remarks>
        public IList<IButlerToolBaseInterface> Spawn(string[] Names);


    }


    /// <summary>
    /// This special interface is for the discoverer tool that lets Butler spin up and shutdown tools in its cache
    /// </summary>
    public interface ButlerTool_Discoverer : IButlerSystemToolInterface, IButlerToolSpinup
    {
        /// <summary>
        /// Assign the tool box to make tools live in.
        /// </summary>
        /// <param name="toolBox"></param>
        public void AssignToolBox(ButlerToolBench toolBox);
        /// <summary>
        /// Get the current toolbox to make tools live in. If this returns null. That means no tool box is assigned this discover (WHY?) - do that by calling <see cref="AssignToolBox(ButlerToolBench)"/> with the instance you want
        /// </summary>
        public ButlerToolBench? GetToolBox();

        /// <summary>
        /// Clear assigned <see cref="ButlerTool_DiscoverResource"/>
        /// </summary>
        public void ClearDiscoverSource();

        public void AddButlerToolSource(ButlerTool_DiscoverResource e);

        public void AddDefaultButlerSources(IButlerVaultKeyCollection KeyHandler);

        public void AssignButler(ButlerBase Target);

    }
}
