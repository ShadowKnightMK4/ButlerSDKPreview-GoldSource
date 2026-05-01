using ButlerProtocolBase.ToolSecurity;
using ButlerToolContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.ToolSupport
{
    
    /// <summary>
    /// This is thrown if virtual tool max limit is hit as a guard. Can be disabled if needed
    /// </summary>
    public class VirtualTool_NestedOverflow: SecurityException
    {
        public VirtualTool_NestedOverflow()
        {

        }

        public VirtualTool_NestedOverflow(string? message) : base(message)
        {
        }

        public VirtualTool_NestedOverflow(string? message, Exception? inner) : base(message, inner)
        {
        }
    }
    public static class ToolSurfaceFlagChecking
    {
        /// <summary>
        /// The limit to depth of the virtual tool system the code will check. This limit is NOT for the <see cref="LookupToolFlagEx(IButlerToolBaseInterface, out bool, out ToolSurfaceScope, int, int, ref HashSet{IButlerToolBaseInterface}?)"/> routine
        /// </summary>
        /// <remarks>This effects <see cref="LookupToolFlag(IButlerToolBaseInterface, out bool, out ToolSurfaceScope)"/> limits. And by default that routine rejects values less than 1 or greated than int.maxval/2</remarks>
        public static int MaxDepthLevel => _MaxDepthLevel;
        /// <summary>
        /// Set this to enable MaxDepth larger than int.maxvalue/2
        /// </summary>
        public static bool AllowBigCheck => _AllowBigCheck;

        private static bool _AllowBigCheck = false;

        private static int _MaxDepthLevel = 2000;
        /// <summary>
        /// </summary>
        /// <param name="Base"></param>
        /// <param name="Visitedtools"> instance the compare is <see cref="ReferenceEqualityComparer.Instance"/></param>
        /// <returns></returns>
        static ToolSurfaceScope NestedToolScopeCheck(IButlerToolBaseInterface Base, HashSet<IButlerToolBaseInterface>? Visitedtools, int CurrentNext,int MaxNest)
        {
            /* 
             * nesting doll plan:
             * 1. LookupToolFlag calls this.
             * 2. This sees it houses other tools, calls look up flag on each tool, which loops back to 1 if nested again
             * 3. LookupToolFlag ends up with the flags of any tools the tool houses, IF it implemented virtual tool
             * */
            ToolSurfaceScope CombinedScope = ToolSurfaceScope.NoPermissions;
            if (Base is IButlerToolContainsPrivateTools Contents)
            {
                
                var sub_tools =  Contents.GetInterfaces();
                foreach (string key in sub_tools.Keys)
                {
                    IButlerToolBaseInterface subint;
                    ToolSurfaceScope SingleToolScope = ToolSurfaceScope.NoPermissions;
                    bool HasPerm = false;
                    bool HasVisisted = false;
                    subint = sub_tools[key];
                    if (Visitedtools is not null)
                    {
                        HasVisisted = ! Visitedtools.Add(subint);
                    }
                    if (!HasVisisted)
                    {
                        if (LookUpToolFlagInternal(subint, out HasPerm, out SingleToolScope, Visitedtools,CurrentNext, MaxNest))
                        {
                            CombinedScope |= SingleToolScope;
                        }
                        else
                        {
                            CombinedScope = ToolSurfaceScope.MaxAvailablePermissions;
                            return CombinedScope; // no point in adding more bit checks if all permission on
                        }
                    }
                }
                return CombinedScope;
            }
            else
            {
                return ToolSurfaceScope.NoPermissions;
            }
        }
        
        static bool LookUpToolFlagInternal(IButlerToolBaseInterface Tool, out bool AnyPermission, out ToolSurfaceScope Result, HashSet<IButlerToolBaseInterface>? Visited, int CurrentNested, int MaxNested)
        {
            AnyPermission = false;
            Result = ToolSurfaceScope.MaxAvailablePermissions; /* justification here is if we can't find it, we assume worst case 
                                                                * do note that the code that calls this checks if any permission and defaults to 
                                                                * filling in the blank if not there as wanting *this* permission
                                                                */

            unchecked
            {
                if ((CurrentNested > MaxNested))
                {
                    throw new VirtualTool_NestedOverflow("Out of nesting levels. !");
                }
                else
                {
                    CurrentNested++;
                }
            }

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

            Result |= NestedToolScopeCheck(Tool, Visited, CurrentNested, MaxNested);
            return true;

        }

        public static bool LookupToolFlagEx(IButlerToolBaseInterface Tool, out bool AnyPermission, out ToolSurfaceScope Result, int CurrentNest, int MaxNext, ref HashSet<IButlerToolBaseInterface>? Visited)
        {
            if (Visited is null)
            {
              Visited = new();
            }

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

            bool OkToRun = false;
            unchecked
            {
                if (CurrentNest + 1 < CurrentNest)
                {
                    OkToRun = false;
                }
                else
                {
                    if (CurrentNest + 1 > MaxNext)
                    {
                        OkToRun = false;
                    }
                    else
                    {
                        OkToRun = true;
                    }
                }
                CurrentNest++;
            }

            if (!OkToRun)
            {
                throw new VirtualTool_NestedOverflow("Out of nesting levels. !");
            }
            else
            {
                Result |= NestedToolScopeCheck(Tool, Visited, CurrentNest, MaxNext);
            }
            return true;
        }


        /// <summary>
        /// This is a stub that calls <see cref="LookupToolFlagEx(IButlerToolBaseInterface, out bool, out ToolSurfaceScope, int, int, ref HashSet{IButlerToolBaseInterface}?)"/>
        /// </summary>
        /// <param name="Tool">Tool to check</param>
        /// <param name="AnyPermission">We find any permssion on the list ?</param>
        /// <param name="Result">combined result of permssion requested. If non found, returns max level request (and you ensure chat session works denying it or not)</param>
        /// <returns></returns>
        /// <remarks>To adjust the limit this routine enforces, modify </remarks>
        public static bool LookupToolFlag(IButlerToolBaseInterface Tool, out bool AnyPermission, out ToolSurfaceScope Result)
        {
            if (ToolSurfaceFlagChecking.MaxDepthLevel > int.MaxValue/2)
            {
                throw new InvalidOperationException("WARNING: Extreme max depth set. in ToolSurfaceFlagChecking. To use checks larger than Int.Maxvalue/2, use LookupToolFlagEx routine or set the allowbigcheck");
            }
            if (ToolSurfaceFlagChecking.MaxDepthLevel < 1)
            {
                throw new InvalidOperationException("WARNING: MaxDepth for this routine is set to negative for some reason. Ensure it's more 1 or more and less than maxdepth/2 (unless allowbigcheck bool is set also)");
            }
            AnyPermission = false;
            Result = ToolSurfaceScope.MaxAvailablePermissions; /* justification here is if we can't find it, we assume worst case 
                                                                * do note that the code that calls this checks if any permission and defaults to 
                                                                * filling in the blank if not there as wanting *this* permission
                                                                */
            HashSet<IButlerToolBaseInterface>? Walking = new(ReferenceEqualityComparer.Instance);
            return LookupToolFlagEx(Tool, out AnyPermission, out Result, 0, ToolSurfaceFlagChecking.MaxDepthLevel, ref Walking);
            /*
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

            Result |= NestedToolScopeCheck(Tool, null, 0, int.MaxValue);
            return true;
            */
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
        /// <returns>returns true if tool is marked and passed permssion is fully allowed</returns>
        /// <remarks>
        /// If a tool is not marked for scope, it's assumed to Want <see cref="ToolSurfaceScope.AllAccess"/> and filtered per the setting
        /// </remarks>
        public static bool CheckMinRequirements(IButlerToolBaseInterface Tool, ToolSurfaceScope ToolAllowedPermissions)
        {
            bool GotAny;
            HashSet<IButlerToolBaseInterface>? Walking = new(ReferenceEqualityComparer.Instance);
            ToolSurfaceScope ToolReportedNeeds = ToolSurfaceScope.NoPermissions ;
            if (LookupToolFlagEx(Tool, out GotAny, out ToolReportedNeeds, 0, 2000, ref Walking))
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
            HashSet<IButlerToolBaseInterface>? Walking = new(ReferenceEqualityComparer.Instance);
            if (LookupToolFlagEx(Tool, out bool GotAny, out var Result,0, 2000, ref Walking))
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
