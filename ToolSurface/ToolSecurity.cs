using ButlerToolContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.ToolSupport
{
    public static class ToolSurfaceFlagChecking
    {
        
        public static bool LookupToolFlag(IButlerToolBaseInterface Tool, out bool AnyPermission, out ToolSurfaceScope Result)
        {
            AnyPermission = false;
            Result = ToolSurfaceScope.MaxAvailablePermissions; /* justification here is if we can't find it, we assume worst case 
                                                                * do note that the code that calls this checks if any permission and defaults to 
                                                                * filling in the blank if not there as wanting *this* permission
                                                                */
            var AttrCollection = Tool.GetType().GetCustomAttributes<ToolSurfaceCapabilities>();
            foreach (ToolSurfaceCapabilities Attr in AttrCollection)
            {
                if (AnyPermission == false)
                {
                    Result = Attr.SurfaceScope;
                    AnyPermission = true;
                }
                else
                {
                    if (Attr != null)
                    {
                        Result |= Attr.SurfaceScope;
                        AnyPermission = true;
                    }
                }
            }
            if (!AnyPermission)
            {
                Result = ToolSurfaceScope.MaxAvailablePermissions;
                return false;
            }
            return true;
        }

         public static bool HasToolSurfaceFlags(IButlerToolBaseInterface Tool)
           {
            bool Any;
             LookupToolFlag(Tool, out Any, out _);
            return Any;
           }

        /// <summary>
        /// Verify if a tool meets the minimum requirements
        /// </summary>
        /// <param name="Tool"></param>
        /// <param name="ToolAllowedPermissions"></param>
        /// <returns></returns>
        /// <remarks>
        /// If a tool is not marked for scope, it's assumed to Want <see cref="ToolSurfaceScope.AllAccess"/> and filtered per the setting
        /// </remarks>
        public static bool CheckMinRequirements(IButlerToolBaseInterface Tool, ToolSurfaceScope ToolAllowedPermissions)
        {
            bool GotAny;
            ToolSurfaceScope ToolReportedNeeds = ToolSurfaceScope.NoPermissions ;
            if (LookupToolFlag(Tool, out GotAny, out ToolReportedNeeds))
            {
                
                if ((ToolAllowedPermissions & ToolReportedNeeds) == ToolReportedNeeds)
                {
                    return true;
                }
            }
            else
            {
                // the tool is not marked up. Assume standard reading
                return false;
            }
            return false;
        }

        [Flags]
        public enum AccessFlags
        {
            Read,
            Write,



        };

        internal static bool BinaryFlagCheck(IButlerToolBaseInterface Tool, ToolSurfaceScope CheckThis)
        {
            if (LookupToolFlag(Tool, out bool GotAny, out var Result))
            {
                if ((Result & CheckThis) == CheckThis)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// The generic common checker
        /// </summary>
        /// <param name="Tool">Tool to check</param>
        /// <param name="Access">access flags to verify. If not included, the check is skipped (ie no read flag, CheckThisRead isn't done.</param>
        /// <param name="CheckThisRead">This flag is checked if Access Read is set</param>
        /// <param name="CheckThisWrite">this flag is checked if access write is set</param>
        /// <returns>if either of the checks aren't there if requested, returns false</returns>
        internal static bool HasAbilityFlag(IButlerToolBaseInterface Tool, AccessFlags Access, ToolSurfaceScope CheckThisRead, ToolSurfaceScope CheckThisWrite)
        {
            if (LookupToolFlag(Tool, out bool GotAny, out var Result))
            {
                Access = Access & (AccessFlags.Read | AccessFlags.Write); // mask out anything else

                bool WantRead = Access.HasFlag(AccessFlags.Read);
                bool WantWrite = Access.HasFlag(AccessFlags.Write);

                bool HasRead = false;
                bool HasWrite = false;

                if ((Result & CheckThisRead) == CheckThisRead)
                {
                    if (WantRead)
                    {
                        HasRead = true;
                    }
                }

                if ((Result & CheckThisWrite) == CheckThisWrite)
                {
                    if (WantWrite)
                    {
                        HasWrite = true;
                    }
                }

                if ((WantRead))
                {
                    if (!HasRead)
                    {
                        return false;
                    }
                }

                if (WantWrite)
                {
                    if (!HasWrite)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ask if the tool is flagged for Disk Writing
        /// </summary>
        /// <param name="Tool"></param>
        /// <returns>if true, the tool</returns>
        public static bool HasDiskWriteFlag(IButlerToolBaseInterface Tool)
        {
            return HasDiskAbilityFlag(Tool, AccessFlags.Write);
        }
        /// <summary>
        /// Ask if the tool hs flagged for Disk Reading
        /// </summary>
        /// <param name="Tool"></param>
        /// <returns></returns>
        public static bool HasDiskReadFlag(IButlerToolBaseInterface Tool)
        {
            return HasDiskAbilityFlag(Tool, AccessFlags.Read);
        }


        /// <summary>
        /// Ask if the tool has a combination of read/write
        /// </summary>
        /// <param name="Tool"></param>
        /// <param name="Access"></param>
        /// <returns></returns>
        public static bool HasDiskAbilityFlag(IButlerToolBaseInterface Tool, AccessFlags Access)
        {
            return HasAbilityFlag(Tool, AccessFlags.Read, ToolSurfaceScope.DiskRead, ToolSurfaceScope.DiskWrite);
        }


        /// <summary>
        /// Is the tool marked for sending data on a network
        /// </summary>
        /// <param name="Tool"></param>
        /// <returns></returns>
        public static bool HasNetworkWriteFlag(IButlerToolBaseInterface Tool)
        {
            return HasNetworkAbilityFlag(Tool, AccessFlags.Write);
        }
        /// <summary>
        /// Is the tool marked for receiving data on the network
        /// </summary>
        /// <param name="Tool"></param>
        /// <returns></returns>
        public static bool HasNetworkReadFlag(IButlerToolBaseInterface Tool)
        {
            return HasNetworkAbilityFlag(Tool, AccessFlags.Read);
        }
        public static bool HasNetworkAbilityFlag(IButlerToolBaseInterface Tool, AccessFlags Access)
        {
            return HasAbilityFlag(Tool, Access, ToolSurfaceScope.NetworkRead, ToolSurfaceScope.NetworkWrite);
        }

        public static bool HasSystemWriteFlag(IButlerToolBaseInterface Tool)
        {
            return HasSystemAbilityFlag(Tool, AccessFlags.Write);
        }

        public static bool HasSystemReadFlag(IButlerToolBaseInterface Tool)
        {
            return HasSystemAbilityFlag(Tool, AccessFlags.Read);
        }

        public static bool HasSystemAbilityFlag(IButlerToolBaseInterface Tool, AccessFlags Access)
        {
            return HasAbilityFlag(Tool, Access, ToolSurfaceScope.SystemRead, ToolSurfaceScope.SystemWrite);
        }

        public static bool IsPowerTool(IButlerToolBaseInterface Tool)
        {
            return BinaryFlagCheck(Tool, ToolSurfaceScope.DropsIntoUnmanagedCode | ToolSurfaceScope.ArbitraryExecution);
        }

        public static bool IsArbitraryExecutionTool(IButlerToolBaseInterface Tool)
        {
            return BinaryFlagCheck(Tool, ToolSurfaceScope.ArbitraryExecution);
        }

    }
}
