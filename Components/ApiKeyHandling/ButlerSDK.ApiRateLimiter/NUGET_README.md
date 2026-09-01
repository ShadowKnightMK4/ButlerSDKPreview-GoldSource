
# ButlerSDK - Preview 1 - component of ButlerSDK - ApiKeyRateLimiter
* Github is here https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource
* Recommend grabbing ButlerSDK.Core and its varients as needed.
* You can also clone it from its Github.


# ButlerSDK.ApiKeyRateLimiter

`ApiKeyRateLimiter` provides a lightweight, thread-safe mechanism for controlling access to services using two complementary forms of limits:

* **Per-service inventory** — limits how many calls a service may make.
* **Shared budget** — limits the total cost consumed across services.

A service can use either limit independently, both at the same time, or no limiting at all.

The limiter is intended for scenarios where an application needs to enforce usage boundaries around APIs, tools, services, or other metered operations without giving the limiter responsibility for actually executing those operations.

## How It Works

Each service is registered with:

* A service name
* A cost per call
* Its current inventory
* Its maximum/reset inventory
* A `ButlerApiLimitType` describing which limits apply

The shared budget belongs to the entire `ApiKeyRateLimiter` instance.

When a service is charged, its configured cost is deducted from the shared budget and, when `PerCall` limiting is enabled, the requested number of calls is deducted from that service's inventory.

The shared budget is explicitly shared across all registered services.

### Example

```csharp
var limiter = new ApiKeyRateLimiter
{
    SharedBudget = 100m
};

limiter.AddService(
    "WeatherAPI",
    CostPerCall: 2m,
    CurrentInventory: 10,
    MaxInventory: 10,
    LimitKind: ButlerApiLimitType.PerCall | ButlerApiLimitType.SharedBudget);
```

This configuration means:

* The service has 10 available calls.
* Each call costs 2 budget units.
* Calls consume both inventory and shared budget.
* The service cannot consume more inventory than it has.
* The service cannot consume more shared budget than remains available.

For example, charging five calls consumes:

```text
WeatherAPI inventory: 10 → 5
Shared budget:        100 → 90
```

## Limit Types

`ButlerApiLimitType` determines which restrictions apply.

### `none`

No inventory or shared-budget restriction is applied.

A service using `none` is effectively unlimited by this limiter.

### `PerCall`

Each call consumes one unit of the service's inventory.

For example:

```text
Inventory: 10
Charge:     3 calls

Inventory after charge: 7
```

### `SharedBudget`

Each call consumes the configured `CostPerCall` from the limiter's shared budget.

For example:

```text
Shared budget: 100
Cost per call:   2
Charge:          3 calls

Budget consumed: 6
Remaining:       94
```

### Combining limits

`PerCall` and `SharedBudget` can be combined.

```csharp
ButlerApiLimitType.PerCall | ButlerApiLimitType.SharedBudget
```

When both are enabled, a charge must satisfy both restrictions before anything is deducted.

This makes it possible to express policies such as:

> "This API may be called no more than 100 times, and those calls may consume no more than 500 budget units."

## Registering Services

Use `AddService()` to register a service.

```csharp
limiter.AddService(
    "MyService",
    CostPerCall: 5m,
    CurrentInventory: 100,
    MaxInventory: 100,
    LimitKind: ButlerApiLimitType.PerCall |
               ButlerApiLimitType.SharedBudget);
```

By default, adding a service that already exists throws an `InvalidOperationException`.

If replacement is intentional, use `ReplaceIfExists`:

```csharp
limiter.AddService(
    "MyService",
    CostPerCall: 10m,
    CurrentInventory: 50,
    MaxInventory: 50,
    LimitKind: ButlerApiLimitType.PerCall,
    ReplaceIfExists: true);
```

Negative costs are not supported. A zero cost should be used when a service should not consume shared budget.

## Checking Permission Without Charging

Use `CheckForCallPermission()` when you want to determine whether a call is currently affordable without consuming inventory or budget.

```csharp
if (limiter.CheckForCallPermission("MyService"))
{
    // The call can currently be afforded.
}
```

You can also check multiple calls:

```csharp
if (limiter.CheckForCallPermission("MyService", 5))
{
    // Five calls can currently be afforded.
}
```

This method does **not** consume the allowance.

If the service does not exist, `ServiceNonExistentException` is thrown.

## Checking and Charging

For the normal execution path, prefer:

```csharp
CheckForCallPermissionAndCharge()
```

This performs the affordability check and, if permitted, consumes the appropriate budget and inventory. ButlerSDK uses this path in normal execution.

```csharp
if (limiter.CheckForCallPermissionAndCharge("MyService"))
{
    // Execute the service call.
}
else
{
    // The service is currently over its available allowance.
}
```

For multiple calls:

```csharp
if (limiter.CheckForCallPermissionAndCharge("MyService", 3))
{
    // Execute three calls.
}
```

If the call cannot be afforded because of the configured budget or inventory, the method returns `false` and does not consume the allowance.

The class performs the permission check and charge while holding its synchronization lock, preventing another thread from consuming the same allowance between those operations.

## Charging Directly

`ChargeService()` can be used when the caller wants to perform the charge directly.

```csharp
limiter.ChargeService("MyService", 1);
```

Unlike `CheckForCallPermissionAndCharge()`, this method communicates failure through exceptions rather than returning `false`.

For example, an insufficient budget or inventory results in `OverBudgetException`.

```csharp
try
{
    limiter.ChargeService("MyService", 1);
}
catch (ApiKeyRateLimiter.OverBudgetException)
{
    // The requested charge cannot be satisfied.
}
```

## Inspecting Services

You can determine whether a service has been registered:

```csharp
if (limiter.DoesServiceExist("MyService"))
{
    // Service exists.
}
```

The total number of registered services is available through:

```csharp
int count = limiter.ServiceCount;
```

## Reading Inventory and Cost

Current service inventory:

```csharp
decimal inventory =
    limiter.GetServiceInventory("MyService");
```

Configured maximum/reset inventory:

```csharp
decimal limit =
    limiter.GetServiceLimit("MyService");
```

Current cost per call:

```csharp
decimal cost =
    limiter.GetCurrentCost("MyService");
```

If the requested service does not exist, these operations throw `ServiceNonExistentException`.

## Updating a Service

The limiter allows an application's policy to be changed at runtime.

Change the service's maximum/reset inventory:

```csharp
limiter.AssignNewServiceLimit("MyService", 100);
```

Reset the current inventory back to that limit:

```csharp
limiter.ResetServiceLimit("MyService");
```

Change the cost per call:

```csharp
limiter.AssignNewCost("MyService", 2.5m);
```

Costs must be zero or greater.

## Removing a Service

```csharp
limiter.RemoveService("MyService");
```

Removing an unknown service is normally ignored.

If the caller wants removal of an unknown service to be treated as an error:

```csharp
limiter.RemoveService(
    "MyService",
    PanicIfNonExistent: true);
```

This throws `ServiceNonExistentException` when the service is not registered.

## Exceptions

`ApiKeyRateLimiter` exposes three primary exception types.

### `ServiceNonExistentException`

Thrown when an operation requires a service that has not been registered.

Examples include:

```csharp
CheckForCallPermission("UnknownService");
GetServiceInventory("UnknownService");
AssignNewCost("UnknownService", 1);
```

### `InventoryOrSeviceCostException`

Thrown when an invalid inventory or cost operation is attempted.

Examples include negative costs and invalid inventory charges.

A zero-call charge is also restricted when `PerCall` inventory limiting is enabled.

### `OverBudgetException`

Thrown when a requested charge cannot be satisfied by the available shared budget or service inventory.

## Thread Safety

The limiter synchronizes access to its service collection, shared budget, and service inventory using an internal synchronization object.

Operations that inspect or modify limiter state acquire this lock before accessing the underlying state.

This is particularly important for:

```csharp
CheckForCallPermissionAndCharge()
```

because the permission check and resulting charge occur as one synchronized operation rather than as two independently exposed operations.

## A Complete Example

```csharp
var limiter = new ApiKeyRateLimiter
{
    SharedBudget = 50m
};

limiter.AddService(
    "ExpensiveAPI",
    CostPerCall: 5m,
    CurrentInventory: 5,
    MaxInventory: 5,
    LimitKind: ButlerApiLimitType.PerCall |
               ButlerApiLimitType.SharedBudget);

if (limiter.CheckForCallPermissionAndCharge("ExpensiveAPI"))
{
    // Execute the API call.
}
else
{
    Console.WriteLine(
        "ExpensiveAPI is currently unavailable.");
}
```

After ten attempted calls, only the first five can succeed because the service has five units of inventory.

The shared budget provides a second boundary: even if inventory remains available, a call is rejected when its cost would exceed the remaining shared budget.

## Design Intent

`ApiKeyRateLimiter` is deliberately a policy/enforcement component rather than a service execution component.

It does not make API calls, manage API keys, or determine how a service should actually be invoked.

Instead, it answers a narrower question:

> **"Is this operation allowed to consume this much of the application's configured allowance?"**

That makes it useful as a boundary around ButlerSDK tools, API providers, metered services, or other operations where the application needs deterministic usage accounting.

The limiter itself does not decide what a service call means; the host application defines the services, costs, inventories, and applicable limits.


### 📄 License


Apache 2.0 License. See [LICENSE](LICENSE) for details.

Copyright 2025-2026 by Thomas Paul Betterly