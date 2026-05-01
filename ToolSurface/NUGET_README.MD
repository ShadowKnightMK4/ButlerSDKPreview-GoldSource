# ButlerSDK.Tools.ToolSurface

**Version:** v1.1.1-preview | **Platform:** .NET 8  
**Part of the [ButlerSDK](https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource) Ecosystem**

`ButlerSDK.Tools.ToolSurface` provides the core permission-boundary and reflection-scanning engine for the Butler AI Orchestration Framework. It ensures that any C# tool injected into a Butler LLM chat session does not request or execute with higher privileges than the session explicitly allows.

## 🛡️ Core Features

* **Attribute-Based Security:** Declaratively tag your tools with specific required scopes (e.g., `ToolSurfaceScope.NetworkRead`, `ToolSurfaceScope.NoPermissions`).
* **Fail-Secure Defaults:** If a custom tool fails to declare its surface capabilities, the SDK assumes it requires `MaxAvailablePermissions`. If the session isn't explicitly configured for max access, the tool is rejected at AddTool() stage and running the tool stage.

* **Robust Virtual Tool Scanning (New in v1.1.1-preview):** Safely handles `VirtualTool` instances (tools that contain other tools). Includes built-in circular dependency detection to prevent infinite loops and enforces a hard max-nesting depth limit (255) to prevent `StackOverflow` or out-of-memory attacks.

## ⚠️ Security Disclaimer: The Honor System
Currently, the `ToolSurface` relies on the **honor system**. It aggressively scans and enforces the *declared* attributes of a tool, but it does not execute low-level application sandboxing (like eBPF or AppDomains). 

If a third-party tool declares `ToolSurfaceScope.NoPermissions` but contains malicious C# code that attempts to silently zip a directory and email it over the network, standard .NET execution will still allow it. Ensure you trust the underlying C# code of the tools you load into your toolkit.

## 💻 How to Use

You simply need to tag your tools. If you are using `ButlerSDK.Core`, the toolkit's `AddTool()` pipeline automatically handles the surface sweeping, platform checking, and initialization for you.

```csharp
using ButlerProtocolBase.ToolSecurity;
using ButlerSDK.ToolSupport;

// 1. Tag the tool with its required capability boundary[ToolSurfaceCapabilities(ToolSurfaceScope.StandardReading)]
public class MyCustomTool : ButlerToolBase
{
    public override string ToolName => "MyCustomTool";
    public override string ToolDescription => "Reads standard application data.";
    
    public MyCustomTool(IButlerVaultKeyCollection key) : base(key) { }

    // Tool execution logic here...
}