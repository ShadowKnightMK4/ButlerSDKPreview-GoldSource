
# ApiKeyRateLimiter

A lightweight, in-memory rate limiter and budget enforcer for API calls inside **ButlerSDK**.

`ApiKeyRateLimiter` lets you:

* Track multiple named services
* Enforce per-call inventory limits
* Enforce a shared monetary/token budget
* Combine both limit types
* Charge services safely with explicit exception handling

This component is part of ButlerSDK and inherits its license.

*Important*: This limiter does not actually execute or run the services, it just tracks the limits and offers a way to check if out of budget.

  
---

## Why This Exists

When working with multiple AI providers (OpenAI, Gemini, Ollama, etc.), you often need:

* A **shared cost budget** (e.g., $5 total usage cap)
* A **per-service call inventory** (e.g., 100 calls per provider)
* A way to **fail fast** before overspending

This class provides deterministic, explicit control over those constraints.

It is:

* In-memory
* Thread-safe for `SharedBudget`
* Explicit in failure modes
* Designed for integration into higher-level orchestration

---

## Core Concepts

Each service you register has:

| Property      | Meaning                      |
| ------------- | ---------------------------- |
| `ServiceName` | Unique key like 'MAPKEY'     |
| `CostPerCall` | Deducted from `SharedBudget` |
| `Inventory`   | Calls remaining              |
| `Reset`       | Max inventory reset value    |
| `LimitType`   | How limits are enforced      |

---

## Limit Types

```csharp
[Flags]
public enum LimitType
{
    none,
    PerCall = 1,
    SharedBudget = 2
}
```

You can combine flags:

* `LimitType.PerCall`
* `LimitType.SharedBudget`
* `LimitType.PerCall | LimitType.SharedBudget`
* `LimitType.none` (disables enforcement)

---

## Quick Example

```csharp
var limiter = new ApiKeyRateLimiter();

// Set shared budget
limiter.SharedBudget = 10.0m;

// Register a service
limiter.AddService(
    ServiceName: "OpenAI",
    CostPerCall: 0.25m,
    CurrentInventory: 100,
    MaxInventory: 100,
    LimitKind: ApiKeyRateLimiter.LimitType.PerCall 
               | ApiKeyRateLimiter.LimitType.SharedBudget
);

// Check before charging
if (limiter.CheckForCallPermission("OpenAI", 2))
{
    limiter.ChargeService("OpenAI", 2);
}
```

---

## What Happens on Failure?

Two explicit exceptions exist:

### `ServiceNonExistentException`

Thrown when:

* Charging a service that wasn’t registered
* Querying unknown service data

### `OverBudgetException`

Thrown when:

* Shared budget would go negative on charge
* Inventory would go negative on charge

This ensures callers must handle exhaustion scenarios intentionally.

### InventoryOrSeviceCostException

Thrown when attempting to Charge negative inventory or 0.
Thornw when attempting to set a shared budget cost to be less than 0.

This helps ensure charging does NOT increase the budget reseves.

**

---

## Service Management

### Add a service

```csharp
limiter.AddService("Gemini", 0.15m, 50, 50, 
    ApiKeyRateLimiter.LimitType.SharedBudget);
```

### Remove a service

```csharp
limiter.RemoveService("Gemini");
```

### Reset inventory

```csharp
limiter.ResetServiceLimit("Gemini");
```

### Update cost

```csharp
limiter.AssignNewCost("Gemini", 0.20m);
```

---

## Threading Notes

* `SharedBudget` access is synchronized.
* Inventory modifications are not globally locked.
* Designed for controlled orchestration layers, not high-contention parallel mutation.

If you need distributed or persistent rate limiting, this is not that tool.

---

## Design Philosophy

This class favors:

* Explicit state
* Explicit failure
* No background timers
* No silent retries
* No hidden async magic

It is deterministic and predictable by design.

---

## When To Use This

Use it when:

* You are building an AI orchestration layer
* You need cost caps
* You need per-service quotas
* You want hard stops, not soft throttling

Do not use it if you need:

* Distributed coordination
* Sliding window rate limiting
* Time-based throttling
* Persistent tracking across restarts

---

## What's new?
```
        public bool CheckForCallPermissionAndCharge(string ServiceName, int CallCount = 1);
```

This routine makes the check for call permssion and charges it if ok. Returns true if sucess and false if it would go over budget.
Why does this exist: to prevent multh thread cases of Checking the budget *another thread* makes the charge and we're over budget. This routine uses the sync object.
The door is closed on attempting to add services with negative shared budget cost, and charge 0 or negative amounts of inventory.
