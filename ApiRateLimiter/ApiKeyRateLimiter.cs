/*
 * This particular version of ApiKeyRateLimiter  (ref on my LinkedIn account as MIT)
 * is NOT part part of the MIT release there. It's a component of ButlerSDK here and
 * inherits Butler's LICENCE.
  */

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace ButlerSDK
{
    /// <summary>
    /// A quick primer on this class. It collects a Service description with its <see cref="AddService(string, decimal, ulong, ulong, ApiKeyRateLimiter.ButlerApiLimitType, bool)"/> call. In that you define how to limit, an inventory of calls and a cost per call that gets deducted from the shared budget. Inventory is unique for each service.
    /// </summary>
    public class ApiKeyRateLimiter : IApiKeyRateLimiterAtomicCharge
    {
        /// <summary>
        /// Thrown if trying to get info on a service the class doesn't know about.
        /// </summary>
        public class ServiceNonExistentException : Exception
        {
            public ServiceNonExistentException(string message) : base(message) { }
            public ServiceNonExistentException(string message, Exception Inner) : base(message, Inner) { }

            public ServiceNonExistentException()
            {
            }
        }

        /// <summary>
        /// This triggers if attempting to assign a shared budget cost of less than 0, or attempting to dedect a negative amouont of inventory
        /// </summary>
        public class InventoryOrSeviceCostException: Exception
        {
            public InventoryOrSeviceCostException(string message) : base(message) { }
            public InventoryOrSeviceCostException(string message, Exception Inner) : base(message, Inner) { }

            public InventoryOrSeviceCostException()
            {
            }
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

            public OverBudgetException()
            {
            }
        }

        /// <summary>
        /// The Shared Budget is the budget that is shared across all services. If a service has a cost per call, then that cost gets deducted from the shared budget when the service is charged. If the shared budget is not enough to cover the cost of the call, then the call is not allowed and an exception is thrown. 
        /// </summary>
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

        public int ServiceCount => Data.Count;

        Dictionary<string, ApiEntry> Data = new();

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
            if (Data.TryGetValue(ServiceName, out ApiEntry? ret))
            {
                return ret;
            }
            else
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
            if (CallCount < 0)
            {
                throw new InventoryOrSeviceCostException($"{Service.ServiceName} IsCallAfforded check with negative call count is not supported");
            }
            
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
            if (CostPerCall < 0)
            {
                throw new InventoryOrSeviceCostException($"{ServiceName} add attempt with a negative cost per call. This is not supported. If you ment it to not cost anything per call, use 0 instead.");
            }
            lock (SynchObject)
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
        }

        public void RemoveService(string ServiceName, bool PanicIfNonExistent=false)
        {
            lock (SynchObject)
            {
                bool check = Data.Remove(ServiceName, out var _);
                if (!check && PanicIfNonExistent)
                {
                    throw new ServiceNonExistentException($"Attempt to Remove Unknown Service of name {ServiceName}");
                }
            }
        }

        /// <summary>
        /// Internal routine used by <see cref="ChargeService(string, int)"/>
        /// </summary>
        /// <param name="CallCharge">OUTPUT: price to charge charged budget</param>
        /// <param name="CallNumber">inventory to charge the service</param>
        /// <param name="Inv">OUTPUT: inventory to adjust budget</param>
        /// <param name="BudgetOk">set to true if Budget was not overflow, false otherwise</param>
        /// <param name="InventoryOk">set to true if Inventory was not underflow, false otherwise</param>
        /// <param name="Service">Service to charge</param>
        /// <exception cref="OverBudgetException"></exception>
        /// <exception cref="ArgumentNullException">If Passed service is null</exception>
        /// <exception cref="NegativeInventoryOrSeviceCostException"> if sevice cost is negative, or inventory is negative</exception>
        internal bool ChargeServiceCalculation(ref decimal CallCharge, decimal CallNumber, ref decimal Inv, ref bool BudgetOk, ref bool InventoryOk, ApiEntry Service)
        {
            /*
             * This routine calculates the charge(s), updated the ref variables.
             * IT DOES NOT mutate the passed service, that's ChargeService() or the AtomicUpgrade
             */
            ArgumentNullException.ThrowIfNull(Service);
            BudgetOk = InventoryOk = false;

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
                if (Inv == 0)
                {
                    throw new InventoryOrSeviceCostException($"{Service.ServiceName} attempt to deduct 0 inventory with charge per call. This isn't supported. minimum dedect is 1 if Charging per call");
                }
            }
            else
            {
                Inv = 0;
            }

            if (Service.CostPerCall < 0)
            {
                throw new InventoryOrSeviceCostException($"{Service.ServiceName} has negative cost per call. This is not supported. Ensure cost 0 or greater");
            }
            if (Inv < 0)
            {
                throw new InventoryOrSeviceCostException($"{Service.ServiceName} has negative inventory request. This is not supported. Ensure positive numbers for inventory or skip that");
            }

            if (SharedBudget - CallCharge < 0)
            {
                throw new OverBudgetException($"Calling Tool or Service {Service.ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase budget in the software.");
            }
            else
            {
                BudgetOk = true;
            }
            if (Service.Inventory - Inv < 0)
            {
                throw new OverBudgetException($"Calling Tool or Service {Service.ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase inventory in the software.");
            }
            else
            {
                InventoryOk = true;
            }

            if (InventoryOk && BudgetOk)
            {
                return true;
                /*SharedBudget -= CallCharge;
                Service.Inventory -= Inv;*/
            }
            else
            {
                if (!BudgetOk)
                {
                    throw new OverBudgetException($"Calling Tool or Service {Service.ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase budget in the software.");
                }
                if (!InventoryOk)
                {
                    throw new OverBudgetException($"Calling Tool or Service {Service.ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase inventory in the software.");
                }
            }
            return false;

        }
        public void ChargeService(string ServiceName, int CallNumber)
        {
            lock (SynchObject)
            {
                ApiEntry? Service = LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Attempt to Charge Unknown Service of name {ServiceName} {CallNumber} number of calls. Ensure the service is defined fist ");
                }
                else
                {
                    bool BudgetOk = false;
                    bool InventoryOk = false;
                    decimal CallCharge=0;
                    decimal Inv=0;
                    if (ChargeServiceCalculation(ref CallCharge, CallNumber, ref Inv,ref BudgetOk, ref InventoryOk, Service))
                    {
                        if (BudgetOk)
                        {
                            SharedBudget -= CallCharge;
                        }
                        if (InventoryOk)
                        {
                            Service.Inventory -= Inv;
                        }
                    }

                    /*
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
                        BudgetOk = true;
                    }
                    if (Service.Inventory - Inv < 0)
                    {
                        throw new OverBudgetException($"Calling Tool or Service {ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase inventory in the software.");
                    }
                    else
                    {
                        InventoryOk = true;
                    }

                    if (InventoryOk && BudgetOk)
                    {
                        SharedBudget -= CallCharge;
                        Service.Inventory -= Inv;
                    }
                    else
                    {
                        if (!BudgetOk)
                        {
                            throw new OverBudgetException($"Calling Tool or Service {ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase budget in the software.");
                        }
                        if (!InventoryOk)
                        {
                            throw new OverBudgetException($"Calling Tool or Service {ServiceName} {CallNumber} of times is outside of budget. Cannot do it. Increase inventory in the software.");
                        }
                    }
                    */
                    /*
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
                    }*/
                }
            }
        }


     
        public bool CheckForCallPermission(string ServiceName, int CallCount=1)
        {
            lock (SynchObject)
            {
                ApiEntry? Service = LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                return IsCallAfforded(Service, CallCount);
            }
        }


       
        public bool DoesServiceExist(string ServiceName)
        {
            lock (SynchObject)
            {
                return this.LookUpService(ServiceName) != null;
            }
        }

    
        public void AssignNewServiceLimit(string ServiceName, ulong Limit)
        {
            lock (SynchObject)
            {
                var Service = this.LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                else
                    Service.Reset = Limit;
            }
        }

        
        public decimal GetServiceLimit(string ServiceName)
        {
            lock (SynchObject)
            {
                var Service = this.LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                else
                    return Service.Reset;
            }
        }

      
        public void ResetServiceLimit(string ServiceName)
        {
            lock (SynchObject)
            {
                var Service = this.LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                else
                    Service.Inventory = Service.Reset;
            }
        }

        
        public decimal GetServiceInventory(string ServiceName)
        {
            lock (SynchObject)
            {
                var Service = this.LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                else
                    return Service.Inventory;
            }
        }

       
        public void AssignNewCost(string ServiceName, decimal Cost)
        {
            if (Cost < 0)
            {
                throw new InventoryOrSeviceCostException($"{ServiceName} add attempt with a negative cost per call. This is not supported. If you ment it to not cost anything per call, use 0 instead.");
            }
            lock (SynchObject)
            {

                var Service = this.LookUpService(ServiceName);
                if (Service is null)
                {
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
                }
                else
                    Service.CostPerCall = Cost;
            }
        }

      

        public decimal GetCurrentCost(string ServiceName)
        {
            lock (SynchObject)
            {
                var Service = this.LookUpService(ServiceName);
                if (Service is not null)
                {
                    return Service.CostPerCall;
                }
                else
                    throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
        }


        /// <summary>
        /// This will see if the charge is valided and charge the service.  
        /// </summary>
        /// <param name="ServiceName"></param>
        /// <param name="CallCount">int larger than 0</param>
        /// <returns>true if charge went thru and false if non</returns>
        /// <exception cref="ServiceNonExistentException"></exception>
        /// <exception cref="ApiKeyRateLimiter.InventoryOrSeviceCostException">If you try to charge call count less than 0</exception>
        /// <remarks>Thank you for the idea win32 api routine FreeLibraryAndExitThread. </remarks>
        public bool CheckForCallPermissionAndCharge(string ServiceName, int CallCount = 1)
        {
            bool HasCharged = false;
            if (!DoesServiceExist(ServiceName))
            {
                throw new ServiceNonExistentException($"Service {ServiceName} doesn't exist.");
            }
            lock (SynchObject)
            {
                ApiEntry? Service = this.LookUpService(ServiceName);
                if (Service is null)
                    return false;

                try
                {
                    if (IsCallAfforded(ServiceName, CallCount))
                    {


                        bool BudgetOk = false;
                        bool InventoryOk = false;
                        decimal CallCharge = 0;
                        decimal Inv = 0;
                        if (ChargeServiceCalculation(ref CallCharge, CallCount, ref Inv, ref BudgetOk, ref InventoryOk, Service))
                        {
                            if (BudgetOk)
                            {
                                SharedBudget -= CallCharge;
                            }
                            if (InventoryOk)
                            {
                                Service.Inventory -= Inv;
                            }
                        }
                        HasCharged = true;
                    }
                }
                catch (OverBudgetException)
                {
                    HasCharged = false;
                }
                return HasCharged;
            }
        }

    }
}
