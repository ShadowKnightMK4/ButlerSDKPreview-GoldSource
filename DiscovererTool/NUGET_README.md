

# ButlerSDK.Tools.DiscovererTool

[![NuGet](https://img.shields.io/badge/nuget-v1.0.3--preview-blue.svg)](https://www.nuget.org/packages/ButlerSDK.Tools.DiscovererTool/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

**Dynamic, on-demand tool loading for ButlerSDK.**

The **Discoverer** allows an LLM to "shop" for the tools it needs, rather than carrying every tool in its context window at all times. It is a critical component for managing token costs in large agentic systems.

---

## 🚀 Why use Discoverer?

**The Problem: Context Pollution**
If you have a library of 300+ tools (Calendar, Network, FileSystem, Database, etc.), adding all of them to the LLM's system prompt consumes massive amounts of tokens. This increases cost and confuses the model.

**The Solution: Dynamic Loading**
The Discoverer creates a specific workflow:
1.  **Search:** The LLM asks, "Do we have a tool to check the network?"
2.  **Activate:** Discoverer scans available resources, finds `GetPublicIP`, and spins it up.
3.  **Execute:** The tool is added to the active `Butler` session for use.
4.  **Wind-down:** When the task is done, the tools can be unloaded to free up context.

*Note: This pattern works best with high-intelligence Cloud Models (GPT-4o, Gemini 1.5 Pro).*

---

## 🛠 How it Works

The default implementation (`DefaultButlerTool_DiscoverResource`) uses **Reflection** to scan your currently loaded assemblies for tools.

**Scanning Logic:**
It will automatically find and instantiate any class that meets these criteria:
1.  Implements `IButlerToolBaseInterface` (The core contract).
2.  Is **Public** and **Concrete** (Not Abstract).
3.  Is **NOT** tagged with `[ButlerTool_DiscoverAttributes(DisableDiscover = true)]`.

### Custom Discovery Sources
You are not limited to Reflection. You can implement `ButlerTool_DiscoverResource` to load tools from a database, a plugin folder, or a remote API, provided your source can return instantiated `IButlerToolBaseInterface` objects.

```csharp
// Example: Adding a custom discovery source
var myDiscoverer = new ButlerTool_DiscoverTools(keyVault);
myDiscoverer.AddButlerToolSource(new MyDatabaseToolSource());
```

### Adding Discover to a Butler Instance

```csharp
// 1. Setup your KeyVault
var keyVault = new EnvironmentApiKeyMgr(); // or your own implementation

// 2. Initialize Butler
var butler = new Butler(/* parameters */);

// 3. Configure the Discoverer
var discoverer = new ButlerTool_DiscoverTools(keyVault);
discoverer.AssignButler(butler); // Link it to the session

// 4. Add Sources (Where to look for tools)
discoverer.AddDefaultButlerSources(keyVault); // Adds the Reflection scanner
discoverer.AddButlerToolSource(new MyVerifiedPluginScanner()); // Optional custom source

// 5. Register the Discoverer as a Tool so the LLM can use it
butler.TheToolBox = discoverer; 
butler.AddTool(butler.TheToolBox);
```

---

## ⚠️ Versioning & Breaking Changes

**v1.0.3-preview Update:**
To improve modularity, the core interfaces have moved.
*   `DefaultButlerTool_DiscoverResource` and `ButlerTool_Discoverer` now rely on contracts defined in **ButlerSDK.Core.Abstractions**.
*   Ensure your Core and Tool packages are version-aligned to avoid type mismatches.
* They have also been renamed to to be IButlerTool_Discoverer and IButlerTool_DiscoverResource to be more typical interface names.
---

### 📄 License

Apache 2.0 License. See [LICENSE](LICENSE) for details.

Copyright 2025-2026 Thomas Paul Betterly.
