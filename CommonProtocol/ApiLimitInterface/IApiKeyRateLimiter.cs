/*
 * This particular version of ApiKeyRateLimiter  (ref on my LinkedIn account as MIT)
 * is NOT part part of the MIT release there. It's a component of ButlerSDK here and
 * inherits Butler's LICENCE.
  */

namespace ButlerSDK
{
    public interface IApiKeyRateLimiter
    {
        /// <summary>
        /// How many services is this collection currently tracking?
        /// </summary>
        int ServiceCount { get; }
        /// <summary>
        /// The Global Shared budget for this class. The CostPerCall used in <see cref="AddService(string, decimal, ulong, ulong, LimitType, bool)"/> is what this is the budget for. Events all services for this class.
        /// </summary>
        decimal SharedBudget { get; set; }

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
        void AddService(string ServiceName, decimal CostPerCall, ulong CurrentInventory, ulong MaxInventory, ButlerApiLimitType LimitKind, bool ReplaceIfExists = false);
        
        /// <summary>
        /// Assign a new cost deducted from the shared budget for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to assign a new cost too</param>
        /// <param name="Cost">the new cost. Note this is deducted per call from <see cref="SharedBudget"/> when charging a service call</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        void AssignNewCost(string ServiceName, decimal Cost);

        /// <summary>
        /// Update a service inventory limit
        /// </summary>
        /// <param name="ServiceName">name of the service to update</param>
        /// <param name="Limit">the new upper inventory limit</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        void AssignNewServiceLimit(string ServiceName, ulong Limit);

        /// <summary>
        /// Charge the service call
        /// </summary>
        /// <param name="ServiceName">name of the service to charge</param>
        /// <param name="CallNumber">number of calls to charge for</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if a service named ServiceName is not found</exception>
        /// <exception cref="OverBudgetException">Is thrown if charging the call would cause the service to go over budget</exception>
        void ChargeService(string ServiceName, int CallNumber);


     

        /// <summary>
        /// Return if we would stay in budget with an arbitrary number of calls for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to check</param>
        /// <param name="CallCount">Number of calls to account for</param>
        /// <returns>true if correct and false if not. </returns>
        /// <exception cref="ServiceNonExistentException"> is thrown if the service is not found</exception>
        bool CheckForCallPermission(string ServiceName, int CallCount=1);

        /// <summary>
        /// Does an entry for this Service exist in our system?
        /// </summary>
        /// <param name="ServiceName">name of the service to check</param>
        /// <returns>true if it exists or false if not</returns>
        bool DoesServiceExist(string ServiceName);
        /// <summary>
        /// Retrieve the current cost per call for this service
        /// </summary>
        /// <param name="ServiceName">name of the service to assign a new cost too</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        decimal GetCurrentCost(string ServiceName);

        /// <summary>
        /// Get the current remaining inventory for a service
        /// </summary>
        /// <param name="ServiceName">name of the service to get info from</param>
        /// <returns>returns the remaining number of calls left before going over budget</returns>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        decimal GetServiceInventory(string ServiceName);

        /// <summary>
        /// Get the current service inventory limit
        /// </summary>
        /// <param name="ServiceName">name of the service to fetch data from</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>

        decimal GetServiceLimit(string ServiceName);

        /// <summary>
        /// Remove this service from our list.
        /// </summary>
        /// <param name="ServiceName">name of the service to remove</param>

        //void RemoveService(string ServiceName);

        /// <summary>
        /// Remove this service from our list.
        /// </summary>
        /// <param name="ServiceName">name of the service to remove</param>
        /// <param name="PanicIfNonExistent">Throw <see cref="ServiceNonExistentException"/> if the requested service isn't found</param>


        void RemoveService(string ServiceName, bool PanicIfNonExistent=false);

        /// <summary>
        /// Reset the inventory to the max for a service
        /// </summary>
        /// <param name="ServiceName">name of the service on whom to reset</param>
        /// <exception cref="ServiceNonExistentException">Is thrown if the service doesn't exist</exception>
        void ResetServiceLimit(string ServiceName);
    }
}