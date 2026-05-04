***

```markdown
# ButlerSDK.ToolSupport (Preview 1)

**A capability-based security and routing engine for LLM/AI Tool calling in .NET.**

This package is a core component of the **ButlerSDK** AI orchestration framework. It provides the foundational security attributes, interfaces, and validation engines that allow you to treat AI-driven tools as **untrusted components**, forcing explicit permission declarations before an LLM is allowed to execute them.

 ⚠️ **CRITICAL SECURITY ADVISORY (Update Highly recommended. Required)**
 
 **Versions prior to `v1.1.1-preview` are deprecated and unsupported.** Previous versions contained a non zero Denial of Service (DoS) vulnerability chance. Improperly configured or maliciously crafted circular `VirtualTool` dependencies could cause a fatal, uncatchable `StackOverflowException`, immediately crashing the host process or deadlocking it. 
 **Version `1.1.1-preview` and later** introduces a hardened graph-traversal engine with cycle detection (`HashSet`-based reference tracking) and strict recursion depth limits (default 2000) that safely throw a catchable `VirtualTool_NestedOverflow` exception. **All users are highly recommanded to upgrade to this version.**. If you must not, it is recommanded to either avoid VirtualTool design or ensure they are vetted against self referencing loops for example.

---

## 🛡️ The Concept: "Zero-Trust" AI Tools

When giving Large Language Models (LLMs) the ability to execute C# code, you need a blast shield. Instead of letting the AI run blind, this package introduces **Tool Surface Scopes**. 

Developers tag their tools with explicit capabilities (e.g., `NetworkRead`, `DiskWrite`, `ArbitraryExecution`). The host application (or the ButlerSDK Chat Controller) then evaluates these tags against an allowed permission threshold before execution. If a tool requires permissions outside the current allowed scope, execution is blocked.

 🛑 **IMPORTANT CAVEAT: The Honor System**

 Currently, the `[ToolSurfaceCapabilities]` attribute is **declarative**. The framework does not perform static IL analysis or evaluate the actual code inside the tool. This means the current security model operates on an *honor system* regarding the tool's author. Nothing currently stops a maliciously coded tool from declaring `NoPermissions` while containing code that formats a hard drive. 
 
 **Roadmap Teaser:** To transition from an honor system to hard enforcement, the future potential roadmap includes a feature workshopped currently as  **"Blast Doors"**. Essentially, the goal is that the declarative system will be enforced at the OS level rather than relying on the honor system.

### Key Features
* **Declarative Security:** Use the `[ToolSurfaceCapabilities]` attribute to flag exactly what a tool is intended to do.
* **Virtual Tool Unrolling:** Supports "Virtual Tools" (tools that chain or house other nested tools via the https://www.nuget.org/packages/ButlerSDK.Tools.VirtualTool/ nuget design). The engine walks the permssion chain and each tool instance's permissions are only picked once. No Revisits planned.
* **Strict Bitwise Evaluation:** Binary flag checks (`ToolSurfaceFlagChecking`) to verify read/write access to disks, networks, or system layers.

---

## ⚡ Quick Start

**1. Tagging a Tool:**
```csharp
using ButlerProtocolBase.ToolSecurity;
using ButlerToolContract;

// Declare exactly what this tool requires
[ToolSurfaceCapabilities(ToolSurfaceScope.NetworkRead | ToolSurfaceScope.NetworkWrite)]
public class NetworkDiagnosticsTool : IButlerToolBaseInterface
{
    public string ToolName => "NetworkDiagnostics";
    // ... Tool implementation ...
}
```

**2. Validating a Tool's Permissions:**
Before executing a tool requested by the AI, verify it against your application's current security context.

```csharp
using ButlerSDK.ToolSupport;

var myTool = new NetworkDiagnosticsTool();

// Define what the current session is allowed to do
var allowedPermissions = ToolSurfaceScope.NetworkRead | ToolSurfaceScope.NetworkWrite;

// Check if the tool exceeds the allowed permissions
bool isSafeToRun = ToolSurfaceFlagChecking.CheckMinRequirements(myTool, allowedPermissions);

if (!isSafeToRun)
{
    Console.WriteLine("Execution Blocked: Tool requires permissions outside the current scope.");
}
```

---

## 🧩 Part of the ButlerSDK Ecosystem

While this package can be used independently to build your own safe tool-routing logic, it is designed to integrate with the main **ButlerSDK** framework. 

When used alongside `ButlerSDK.Core`, tools validated by this package benefit from:
* 🧥 **"TrenchCoat" Memory:** A sliding-window context engine that ensures your System Prompts and Tool-specific schemas never age out of the LLM's context window.
* 🛠️ **Post-Processing Pipeline (QoS):** Built-in / Opt in "Counter-Measures" that detect tool-call hallucinations (e.g., the model talking about a tool without actually outputting the JSON) and force the AI into a hidden remedial turn to fix its output before the user ever sees it.
* 🔄 **Vendor Agnostic Execution:** Write your capability-safe tools once and run them against OpenAI, Google Gemini, or local models via Ollama.

### Links & Resources
* **GitHub Repository:**[ShadowKnightMK4/ButlerSDKPreview-GoldSource](https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource)
* **Getting Started:** We recommend grabbing `ButlerSDK.Core` and its variants as needed. You can also clone the source directly from GitHub.
* **Bug Reports:** Please file an issue on the GitHub repository or privately email butlersdkfeedback@gmail.com if it shouldn't be public.

### 📄 License

Apache 2.0 License. See [LICENSE](LICENSE) for details.  
Copyright 2025-2026 by Thomas Paul Betterly
```
