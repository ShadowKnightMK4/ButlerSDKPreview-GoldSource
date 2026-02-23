# ButlerSDK - AI Orchestration Framework (Preview 1)

[![NuGet](https://img.shields.io/badge/nuget-v1.0.0--preview-blue.svg)](https://www.nuget.org/packages/ButlerSDK/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Platform: .NET 8](https://img.shields.io/badge/Platform-.NET%208-purple.svg)](https://dotnet.microsoft.com/)

**Robust, vendor-agnostic AI orchestration for .NET.**

ButlerSDK transforms Large Language Models (LLMs) from unpredictable chatbots into reliable infrastructure components. It provides a unified abstraction layer over **OpenAI**, **Google Gemini**, and **Ollama**, while enforcing tool infrastructure, strict C# Tool typing, and opening the door to letting the developer intercept the streaming LLM to steer it or filter for agentic workflows.

---

## 🚀 Why ButlerSDK?

Most AI integrations are simple wrappers around an API. ButlerSDK is an **Orchestrator/Conductor**.

1. **Vendor Agnostic:** Swap between models like `gpt-4o`, `gemini-flash-latest`, and local `mistral` or `gpt-oss-20b` without changing a single line of your business logic.
2. **Self-Healing Agents:** The built-in (and opt-in) **Post-Processing Pipeline** detects tool call hallucinations (e.g., the model *talking* about a tool without actually *calling* it) and forces a "Remedial" turn to fix the error before the user sees it.
3. **Capability-Based Security:** Tools are treated as untrusted. The `ToolSurfaceScope` declarative flags require developers to specify exactly what the tool uses (Disk, Network, OS). The chat session controller has the final say in setting allowed permissions.
4. **"TrenchCoat" Memory:** A sliding-window context engine that ensures System Prompts and Tool-specific directions never age out of the context window. Includes an AuditLog for review.

---

## ⚡ Development Quick Start

ButlerSDK uses a Facade pattern to get you started instantly, while exposing the full architecture for power users.

```csharp
using ButlerSDK;
using ButlerToolContract.DataTypes;

// 1. Initialize the Butler (Facade Pattern)
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt-4o");

// 2. Add Capability-Safe Tools
butler.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime()); // (No special permissions) 

// 3. Adjust Security Scope to allow Network access
butler.ToolSurfaceScope += (ToolSurfaceScope.NetworkWrite | ToolSurfaceScope.NetworkRead);
butler.AddTool(new ButlerTool_Network_Ping());   

// 4. Set Directives
butler.AddSystemMessage("You are a helpful network engineer who is ready to ping an IP.");
butler.AddUserMessage("Please ping 8.8.8.8 and tell me anything you can about it.");

// 5. Stream Response with Auto-Tool Execution
var EndReason = await butler.StreamResponseAsync((update, history) => 
{
    // Handle text chunks for UI updates
    foreach(var part in update.ContentUpdate)
    {
        if (!string.IsNullOrEmpty(part.Text))
            Console.Write(part.Text);
    }
    return true;
});

// Write the final audit log to the console
foreach (ButlerChatMessage msg in butler.ChatCollection)
{
    Console.WriteLine(msg.GetCombinedText());
}
```

---

## 🛡️ Architecture & Features

### 1. The Post-Processing "Counter-Measures" (QoS)
LLMs can hallucinate or fail to follow strict JSON schemas. ButlerSDK implements an `IButlerPostProcessorHandler` (specifically `ToolPostProcessing`) that acts as a Quality of Service (QOS) layer.

*   **Detection:** It scans the stream for keywords indicating the AI *wanted* to use a tool but failed to generate the call.
*   **Intervention:** It pauses the stream to the user.
*   **Remediation:** It injects a temporary system prompt: *"[ERROR] You discussed using a tool but did not output the JSON. Retry immediately."*
*   **Result:** The user receives the correct answer, unaware that the agent failed and fixed itself in the background.

```csharp
var FinishReason = Jeeves.StreamResponseAsync(HandlerChatMessageStreamHandler, new ToolPostProcessing(), false, 5, default);
```

### 2. Vendor Abstraction (`IButlerLLMProvider`)
Write your tools once. Run them anywhere. 

| Provider | Status | Tool Support | Streaming |
| :--- | :--- | :--- | :--- |
| **OpenAI** | ✅ Production | ✅ Native | ✅ Accumulation Mode |
| **Google Gemini** | ✅ Beta | ✅ Native | ✅ One-Shot Mode |
| **Ollama** | ✅ Beta | ✅ Native | ✅ Accumulation Mode |

*To implement a custom provider, implement `IButlerChatClient` and `IButlerLLMProvider`. You'll also need to likely translate between Butler's messaging data and your LLM's own.*

### 3. Secure Key Management
ButlerSDK rejects the practice of holding API keys in long-lived `string` variables.
*   **Storage (At Rest):** Keys can be encrypted with DPAPI on Windows via the `WindowsVault`. 
*   **Memory (In Use):** Keys in memory are handled via `SecureString`. The primary goal is encouraging deterministic disposal and reducing the lifetime of plaintext values in managed memory.
*   **Ephemeral Access Pattern:** Tools utilize internal disposal helpers to access keys *only* for the duration of the HTTP request, minimizing the attack surface. 

---

## 🛠️ Defining Tools

Tools in ButlerSDK are strongly typed C# classes. ButlerSDK utilizes **Interface Sniffing** to dynamically execute optimized asynchronous code while maintaining strict base contracts.

```csharp
[ToolSurfaceCapabilities(ToolSurfaceScope.NetworkRead)]
public class GetPublicIPTool : ButlerToolBase, IButlerToolAsyncResolver
{
    public GetPublicIPTool(IButlerVaultKeyCollection key) : base(key) { }

    public override string ToolName => "GetPublicIP";
    public override string ToolDescription => "Retrieves the machine's external IP address.";
    public override string GetToolJsonString() => NoArgJson; 

    // The Optimized Async Execution Path
    public async Task<ButlerChatToolResultMessage?> ResolveMyToolAsync(string? args, string? id, ButlerChatToolCallMessage? call)
    {
        using var client = new HttpClient();
        var ip = await client.GetStringAsync("https://api.ipify.org/");
        return new ButlerChatToolResultMessage(id, ip);
    }

    // Required by base contract. ButlerSDK's ToolResolver sniffs for IButlerToolAsyncResolver 
    // and bypasses this sync wrapper dynamically at runtime.
    public override ButlerChatToolResultMessage? ResolveMyTool(string? args, string? id, ButlerChatToolCallMessage? call)
    {
        return ResolveMyToolAsync(args, id, call).GetAwaiter().GetResult();
    }
}
```

---

## ⚙️ Minimum Requirements & Setup

*   **.NET 8**: [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
*   **Windows 10+** (Optional: Only required if using the native DPAPI Vault system. Non-Windows users can use `EnvironmentApiKeyMgr` or custom implementations).
*   **Local Models:** Require [Ollama](https://ollama.com/download) (Recommended: 20GB+ free disk space, modern GPU).

### 🐙 Ollama Setup
To use local models via the OpenAI wrapper, ensure Ollama runs in OpenAI compatibility mode. Create or edit the config file (`C:\Users\<yourname>\.ollama\config.toml` on Windows) and add:
```toml
[api]
openai_compat = true
```
Restart Ollama (`ollama stop` -> `ollama start`).

**💻 Local Models of Interest:**
*   `hf.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF:Q4_K_M` — Great base target for testing QoS countermeasures.
*   `hf.co/hermes42/Mistral-7B-Instruct-v0.3-imatrix-GGUF:Q4_K_M` — Solid overall agent.
*   `hf.co/unsloth/gpt-oss-20b-GGUF:Q4_K_M` — Recommended if it fits in your GPU memory.

---

## 🤝 State of the Preview & Contributing

ButlerSDK is currently in **Active Preview**. The core architecture is stable, but expect rapid iteration and potential breaking changes as we approach v1.0. 

The main goal of this preview is feedback. I am open to hearing all of it—especially regarding friction points or bugs.

* 🐛 **Bug Reports:** Please file an Issue on GitHub.
* 🚧 **Friction:** Did the setup confuse you? Let me know.
* ✉️ **Email:** butlersdkfeedback@gmail.com

---

### 📄 License

Apache 2.0 License. See [LICENSE](LICENSE) for details.
Copyright 2025-2026 by Thomas Paul Betterly

---

### ⚠️ Limitations & Upcoming Changes
* Version 1 supports text only (no images or audio). 
* **IN-PROGRESS:** `ButlerBase` can now be provided a generic `IButlerChatCollection`. The engine will assume the simpler interface but dynamically utilize the advanced `IButlerTrenchImplementation` (temporary messages, prompt injection) if the passed class supports it.


