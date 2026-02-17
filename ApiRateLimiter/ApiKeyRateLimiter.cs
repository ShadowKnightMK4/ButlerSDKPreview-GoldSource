/*
 * This particular version of ApiKeyRateLimiter  (ref on my LinkedIn account as MIT)
 * is NOT part part of the MIT release there. It's a component of ButlerSDK here and
 * inherits Butler's LICENCE.
  */

namespace ButlerSDK
{
    /// <summary>
    /// A quick primer on this class. It collects a Service description with its <see cref="AddService(string, decimal, ulong, ulong, ApiKeyRateLimiter.LimitType, bool)"/> call. In that you define how to limit, an inventory of calls and a cost per call that gets deducted from the shared budget. Inventory is unique for each service.
    /// </summary>
    public class ApiKeyRateLimiter
    {
        /// <summary>
        /// Thrown if trying to get info on a service the class doesn't know about.
        /// </summary>
        public class ServiceNonExistentException : Exception
        {
            public ServiceNonExistentException(string message) : base(message) { }
            public ServiceNonExistentException(string message, Exception Inner) : base(message, Inner) { }
        }
        /// <summary>
        /// thrown by <see cref="ChargeService(string, int)"/> if the budget or inventory doesn't allow it.
        /// </summary>
        public class OverBudgetException: Exception
        {
            public OverBudgetException(string msg) : base(msg)
            {

            }
            public OverBudgetException(string msg,  Exception inner) : base(msg, inner)
            {

            }

        }
        /// <summary>
        /// The Global Shared budget for this class. The CostPerCall used in <see cref="AddService(string, decimal, ulong, ulong, LimitType, bool)"/> is what this is the budget for. Events all services for this class.
        /// </summary>
        public decimal SharedBudget
        {
           get {
               lock (SynchObject)
                    {
                    return _SharedBudget;

                    }
                }
            set
            {
                lock (SynchObject)
                {
                        _SharedBudget = value;
                }
            }
        }

        /// <summary>
        /// Private object of the budget.
        /// </summary>
        private decimal _SharedBudget;
        /// <summary>
        /// Lock object to help sync.
        /// </summary>
        private object SynchObject = new();

        /// <summary>
        /// A single entry. Note. that this is marked internal in case it needs to change.
        /// </summary>
        internal class ApiEntry
        {
            /// <summary>
            /// Name of the service
            /// </summary>
            public string? ServiceName;
            /// <summary>
            /// How the calls are limited
            /// </summary>
            public LimitType Limit;
            /// <summary>
            /// Price per single call. You pick the unit and keep it the consistent
            /// </summary>
            public decimal CostPerCall;
            /// <summary>
            /// Inventory of the calls. 1 call consumes 1 inventory
            /// </summary>
            public decimal Inventory;
            /// <summary>
            /// Upper Inventory reset cal
            /// </summary>
            public decimal Reset;

        }

        /// <summary>
        /// Defines how the limits work. None Trumps the rest but PerCall and SharedBudget mix OK.
        /// </summary>
        [Flags]

        public enum LimitType
        {
            /// <summary>
            /// no limit. This one trumps all others.
            /// </summary>
            none,
            /// <summary>
            /// Each call dec inv by 1. If 0 is left, further calls fails
            /// </summary>
            PerCall = 1,
            /// <summary>
            /// Each call dec budge by cost per call. If budget can't support it, call fails.
            /// </summary>
            SharedBudget =2
        }
        public ApiKeyRateLimiter()
        {

        }

        /// <summary>
        /// Get the class the service points to or null
        /// </summary>
        /// <param name="ServiceName"></param>
        /// <returns>returns either an <see cref="ApiEntry"/> if found or null otherwise</returns>
        ApiEntry? LookUpService(string ServiceName)
        {
            try
            {
                return Data[ServiceName];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
        /// <summary>
        /// Check if our budget or inventory supports the call
        /// </summary>
        /// <param name="ServiceName">name of the service to look up</param>
        /// <param name="CallCount">how many calls to check for</param>
        /// <returns>if true, we won't go over budget or inventory if the call is charged</returns>
        /// <remarks>With this being a non public routine, some checks are skipped</remarks>
        bool IsCallAfforded(string ServiceName, int CallCount)
        {
            ApiEntry? entry;
            entry = LookUpService(ServiceName);

            if (entry is null)
                return false;
            return IsCallAfforded(entry, CallCount);
        }

        /// <summary>
        /// check if our budget or inv supports this call
        /// </summary>
        /// <param name="Service">Service to check</param>
        /// <param name="CallCount">how many calls to check for</param>
        /// <returns>if true, we won't go over budget or inventory if the call is charged</returns>
        /// <exception cref="ArgumentNullException">Is thrown if Service is null</exception>
        /// <remarks>With this being a non public routine, some checks are skipped</remarks>
        bool IsCallAfforded(ApiEntry Service, int CallCount)
        {
            ArgumentNullException.ThrowIfNull(Service);
            decimal CostCalculated;
            if (CallCount == 0) return true;

            if (Service.Limit == LimitType.none)
                return true;


            if (Service.Limit.HasFlag(LimitType.PerCall))
            {
                if (Service.Inventory - CallCount < 0)
                    return false;
            }

            if (Service.Limit.HasFlag(LimitType.SharedBudget))
                CostCalculated = CallCount * Service.CostPerCall;
            else
                CostCalculated = 0;

            if (SharedBudget - CostCalculated < 0)
                return false;

            return true;
        }

        /// <summary>
        /// Add A new service to track and describe it
        /// </summary>
        /// <param name="ServiceName">name of the service - keep it unique to this instance</param>
        /// <param name="CostPerCall">each call costs this much out of the charged budget. If subtracting this cost from the shared budget leaves less than 0, the call fails</param>
        /// <param name="CurrentInventory">number of items in the unique inventory for this service 1 call = 1 item. If this is 0, the call would fail</param>
        /// <param name="MaxInventory">More like the value CurrentInventory is reset to on refresh</param>
        /// <param name="LimitKind">How we limit the call. Use any combination except that if you use <see cref="LimitType.none"/> the check is disabled</param>
        /// <param name="ReplaceIfExists">Default is false, if true, this new service will override an old one</param>
        /// <exception cref="InvalidOperationException">thrown if attempting to add a duplicate and ReplaceIfExists is false</exception>
        public void AddService(string ServiceName, decimal CostPerCall, ulong CurrentInventory, ulong MaxInventory, LimitType LimitKind, bool ReplaceIfExists=false)
        {
            if (Data.ContainsKey(ServiceName) && ReplaceIfExists == false)
            {
                throw new InvalidOperationException($"The service {ServiceName} does exist already. ");
            }
            ApiEntry NewEntry;
            NewEntry = new ApiEntry()
            {
                ServiceName = ServiceName,
                CostPerCall = CostPerCall,
                Inventory = CurrentInventory,
                Reset = MaxInventory,
                Limit = LimitKind
            };
            Data[ServiceName] = NewEntry;
        }

        /// <summary>
        /// Remove this service from our list.
        /// </summary>
        /// <param name="ServiceName">name of the service to remove</param>
        /// <param name="PanicIfNonExistent">Throw <see cref="ServiceNonExistentException"/> if the requested service isn't found</param>
        public void RemoveService(string ServiceName, bool PanicIfNonExistent)
        {
            bool check = Data.Remove(ServiceName);
            if (!check && PanicIfNonExistent)
            {
                throw new ServiceNonExistentException($"Attempt to Remove Unknown Service of name {ServiceName}");
            }
        }
        /// <summary>
        /// Remove this service from our list.
        /// </summary>
        /// <param name="ServiceName">name of the service to remove</param>
        public void RemoveService(string ServiceName)
        {
            RemoveService(ServiceName, false);
        }

        /// <summary>
        /// Charge the service call
        /// </summary>
        /// <param name="ServiceName">name of the service to charge</param>
        /// <param name="CallNumber">number of calls to charge for</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if a service named ServiceName is not found</exception>
        /// <exception cref="OverBudgetException">Is thrown if charging the call would cause the service to go over budget</exception>
        public void ChargeService(string ServiceName, int CallNumber)
        {

            ApiEntry? Service = LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Attempt to Charge Unknown Service of name {ServiceName} {CallNumber} number of calls. Ensure the service is defined fist ");
            }
            else
            {
                decimal CallCharge;
                decimal Inv;
                if (Service.Limit.HasFlag(LimitType.SharedBudget))
                {
                    CallCharge = Service.CostPerCall * CallNumber;
                }    
                else
                {
                    CallCharge = 0;
                }
                
                if (Service.Limit.HasFlag(LimitType.PerCall))
                {
                    Inv = CallNumber;
                }
                else
                {
                    Inv = 0;
                }
                if (SharedBudget - CallCharge < 0)
                {
                    throw new OverBudgetException($"Calling Tool or Service {ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase budget in the software.");
                }
                else
                {
                    SharedBudget -= CallCharge;
                }
                if (Service.Inventory - Inv < 0)
                {
                    throw new OverBudgetException($"Calling Tool or Service {ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase inventory in the software.");
                }
                else
                {
                    Service.Inventory -= Inv;
                }
            }
        }

        /// <summary>
        /// Return if we would stay in budget with a single call for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to check</param>
        /// <returns>true if correct and false if not. </returns>
        /// <exception cref="ServiceNonExistentException"> is thrown if the service is not found</exception>
        public bool CheckForCallPermission(string ServiceName)
        {
            return CheckForCallPermission(ServiceName, 1);
        }

        /// <summary>
        /// Return if we would stay in budget with an arbitrary number of calls for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to check</param>
        /// <param name="CallCount">Number of calls to account for</param>
        /// <returns>true if correct and false if not. </returns>
        /// <exception cref="ServiceNonExistentException"> is thrown if the service is not found</exception>
        public bool CheckForCallPermission(string ServiceName, int CallCount)
        {
            ApiEntry? Service = LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            return IsCallAfforded(Service, CallCount);
        }


        /// <summary>
        /// Does an entry for this Service exist in our system?
        /// </summary>
        /// <param name="ServiceName">name of the service to check</param>
        /// <returns>true if it exists or false if not</returns>
        public bool DoesServiceExist(string ServiceName)
        {
            return Data.ContainsKey(ServiceName);
        }

        /// <summary>
        /// Update a service inventory limit
        /// </summary>
        /// <param name="ServiceName">name of the service to update</param>
        /// <param name="Limit">the new upper inventory limit</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        public void AssignNewServiceLimit(string ServiceName, ulong Limit)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            else
                Service.Reset = Limit;
        }

        /// <summary>
        /// Get the current service inventory limit
        /// </summary>
        /// <param name="ServiceName">name of the service to fetch data from</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>

        public decimal GetServiceLimit(string ServiceName)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            else
                return Service.Reset;
        }

        /// <summary>
        /// Reset the inventory to the max for a service
        /// </summary>
        /// <param name="ServiceName">name of the service on whom to reset</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        public void ResetServiceLimit(string ServiceName)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            else
                Service.Inventory = Service.Reset;
        }

        /// <summary>
        /// Get the current remaining inventory for a service
        /// </summary>
        /// <param name="ServiceName">name of the service to get info from</param>
        /// <returns>returns the remaining number of calls left before going over budget</returns>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        public decimal GetServiceInventory(string ServiceName)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            else
                return Service.Inventory;
        }

        /// <summary>
        /// Assign a new cost deducted from the shared budget for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to assign a new cost too</param>
        /// <param name="Cost">the new cost. Note this is deducted per call from <see cref="SharedBudget"/> when charging a service call</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        public void AssignNewCost(string ServiceName, decimal Cost)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is null)
            {
                throw new InvalidOperationException($"Service {ServiceName} doesn't exist.");
            }
            else
                Service.CostPerCall = Cost;
        }

        /// <summary>
        /// Retrieve the current cost per call for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to assign a new cost too</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>

        public decimal GetCurrentCost(string ServiceName)
        {
            var Service = this.LookUpService(ServiceName);
            if (Service is not null)
            {
                return Service.CostPerCall;
            }
            else
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
        }

        /// <summary>
        /// How many services is this currently tracking?
        /// </summary>

        public int ServiceCount => Data.Count;

        Dictionary<string, ApiEntry> Data = new();

    }
}
