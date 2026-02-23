/*
 * This particular version of ApiKeyRateLimiter  (ref on my LinkedIn account as MIT)
 * is NOT part part of the MIT release there. It's a component of ButlerSDK here and
 * inherits Butler's LICENCE.
  */

namespace ButlerSDK
{
    /// <summary>
    /// A quick primer on this class. It collects a Service description with its <see cref="AddService(string, decimal, ulong, ulong, ApiKeyRateLimiter.ButlerApiLimitType, bool)"/> call. In that you define how to limit, an inventory of calls and a cost per call that gets deducted from the shared budget. Inventory is unique for each service.
    /// </summary>
    public class ApiKeyRateLimiter : IApiKeyRateLimiter
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
        public class OverBudgetException : Exception
        {
            public OverBudgetException(string msg) : base(msg)
            {

            }
            public OverBudgetException(string msg, Exception inner) : base(msg, inner)
            {

            }

        }
 
        public decimal SharedBudget
        {
            get
            {
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
            public ButlerApiLimitType Limit;
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

            if (Service.Limit == ButlerApiLimitType.none)
                return true;


            if (Service.Limit.HasFlag(ButlerApiLimitType.PerCall))
            {
                if (Service.Inventory - CallCount < 0)
                    return false;
            }

            if (Service.Limit.HasFlag(ButlerApiLimitType.SharedBudget))
                CostCalculated = CallCount * Service.CostPerCall;
            else
                CostCalculated = 0;

            if (SharedBudget - CostCalculated < 0)
                return false;

            return true;
        }

      
        public void AddService(string ServiceName, decimal CostPerCall, ulong CurrentInventory, ulong MaxInventory, ButlerApiLimitType LimitKind, bool ReplaceIfExists = false)
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

        public void RemoveService(string ServiceName, bool PanicIfNonExistent=false)
        {
            bool check = Data.Remove(ServiceName);
            if (!check && PanicIfNonExistent)
            {
                throw new ServiceNonExistentException($"Attempt to Remove Unknown Service of name {ServiceName}");
            }
        }

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
                if (Service.Limit.HasFlag(ButlerApiLimitType.SharedBudget))
                {
                    CallCharge = Service.CostPerCall * CallNumber;
                }
                else
                {
                    CallCharge = 0;
                }

                if (Service.Limit.HasFlag(ButlerApiLimitType.PerCall))
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


     
        public bool CheckForCallPermission(string ServiceName, int CallCount=1)
        {
            ApiEntry? Service = LookUpService(ServiceName);
            if (Service is null)
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            return IsCallAfforded(Service, CallCount);
        }


       
        public bool DoesServiceExist(string ServiceName)
        {
            return Data.ContainsKey(ServiceName);
        }

    
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

     

        public int ServiceCount => Data.Count;

        Dictionary<string, ApiEntry> Data = new();

    }
}
