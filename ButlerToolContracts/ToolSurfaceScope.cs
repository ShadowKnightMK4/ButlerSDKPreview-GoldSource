using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract
{
    
    [Flags]
    public enum ToolSurfaceScope
    {
        
        /// <summary>
        /// The tool calculates something does not touch disk or network or os. Aka NO SIDE EFFECTS
        /// </summary>
        NoPermissions = 0,
        /// <summary>
        /// The tool requests data from network (POST, REST, ect...)
        /// </summary>
        NetworkRead = 1,
        /// <summary>
        /// The tool accesses a local disk system
        /// </summary>
        DiskRead = 2,
        /// <summary>
        /// The tool is reading system data like asking OS for info
        /// </summary>
        SystemRead = 4,

        /// <summary>
        /// The tool sends data over the network (POST, REST, ect...)
        /// </summary>
        NetworkWrite = 8,
        /// <summary>
        /// The tool writes to the local disk system
        /// </summary>
        DiskWrite = 16,
        /// <summary>
        /// The tool makes a change to the OS running 
        /// </summary>
        SystemWrite = 32,

        /// <summary>
        /// The tool drops into unmanaged code such as calling a DLL, COM object, ect.... Aka PInvoke
        /// </summary>
        DropsIntoUnmanagedCode = 64,

        /// <summary>
        /// The tool invokes something that the LLM can directly send code to execute such as a code interpreter or a shell execution environment (CMD/ PowerShell, bash , ect...)
        /// </summary>
        /// <remarks>Warning a tool that indeed does *not* examine the output of the LLM before running it is at risk of damaging the os</remarks>
        ArbitraryExecution = 128,

        



        

        /// <summary>
        /// Easy way to specify standard read and write permissions
        /// </summary>
        StandardReading = NetworkRead | DiskRead | SystemRead,
        /// <summary>
        /// Easy way to specify standard read and write permissions
        /// </summary>
        StandardWriting = NetworkWrite | DiskWrite | SystemWrite,

        /// <summary>
        /// A combo of unmanaged code and arbitrary execution. This is a powerful combo that should be used with care.
        /// </summary>
        PowerTool = DropsIntoUnmanagedCode | ArbitraryExecution,
        /// <summary>
        /// PowerTool plus standard read and write
        /// </summary>
        AllAccess = StandardReading | StandardWriting | DropsIntoUnmanagedCode | ArbitraryExecution,

        /// <summary>
        ///  all possible permissions now. Use with caution
        /// </summary>
#pragma warning disable CA1069 // Enums values should not be duplicated
            // reason for suppression is while AllAccess is what it is, MaxAvailable here will grow as needed when more permissions added.
        MaxAvailablePermissions = NetworkRead | NetworkWrite | DiskRead | DiskWrite | SystemRead | SystemWrite | DropsIntoUnmanagedCode | ArbitraryExecution
#pragma warning restore CA1069 // Enums values should not be duplicated
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ToolSurfaceCapabilities: Attribute
    {
        public ToolSurfaceCapabilities(ToolSurfaceScope surfaceScope)
        {
            this.SurfaceScope = surfaceScope;
        }
        public ToolSurfaceScope SurfaceScope { get; set; }
    }
}
