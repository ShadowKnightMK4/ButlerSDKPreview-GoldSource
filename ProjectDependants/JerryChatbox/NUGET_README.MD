
# ButlerSDK - AI Orchestration Framework (Preview 1)

**Robust, vendor-agnostic AI orchestration for .NET.**

ButlerSDK transforms Large Language Models (LLMs) from unpredictable chatbots into reliable infrastructure components. It provides a unified abstraction layer over **OpenAI**, **Google Gemini**, and **Ollama**, while enforcing tool infrastructure, strict C# typing, and dynamic streaming interception for agentic workflows.

---

### 🚀 Why ButlerSDK?
Most AI integrations are simple wrappers. ButlerSDK is a complete **Orchestrator**.

*   🔌 **Vendor Agnostic:** Swap between `gpt-4o`, `gemini-flash`, and local `mistral` models without changing a single line of business logic.
*   🧠 **Self-Healing Agents (QoS):** Built-in (And Opt in) Post-Processing detects tool-call hallucinations (e.g., the model discussing a tool without outputting the JSON) and forces a hidden "Remedial" turn to fix the error mid-stream.
*   🛡️ **Capability-Based Security:** Tools are sandboxed. The `ToolSurfaceScope` flags require developers to specify exact permissions (Disk, Network, OS execution). 
*   🗄️ **"TrenchCoat" Memory:** A tiered, sliding-window context engine that ensures your System Prompts and Tool instructions are permanently pinned and never age out of the context window.

---

### ⚡ Quick Start

ButlerSDK uses a Facade pattern to get you started instantly, while exposing the full architecture for power users.

```csharp
using ButlerSDK;
using ButlerToolContract.DataTypes;

// 1. Initialize the Butler
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt-4o");

// 2. Adjust Security Scope & Add Capability-Safe Tools
butler.ToolSurfaceScope += (ToolSurfaceScope.NetworkWrite | ToolSurfaceScope.NetworkRead);
butler.AddTool(new ButlerTool_Network_Ping());   

// 3. Set Directives
butler.AddSystemMessage("You are a helpful network engineer.");
butler.AddUserMessage("Please ping 8.8.8.8 and summarize the results.");

// 4. Stream Response with Auto-Tool Execution
var EndReason = await butler.StreamResponseAsync((update, history) => 
{
    foreach(var part in update.ContentUpdate)
    {
        Console.Write(part.Text); 
    }
    return true;
});
```


### 🔐 Secure Key Management & Ephemeral Access

ButlerSDK rejects the practice of holding API keys in long-lived string variables.

Keys are handled in memory via SecureString to reduce the lifetime of plaintext values.

Tools can utilize an Ephemeral Access Pattern, accessing keys only for the duration of the HTTP request, minimizing the attack surface.

Encrypted-at-rest DPAPI Vaulting is available via the WindowsVault package.

For Non Windows, there's support for loading API keys with the EnvironmentApiKeyMgr in the ButlerSDK.ApiKeyMgr nuget.



### 📚 Full Documentation & Source Code

ButlerSDK is an open-source project. For the full documentation—including how to write custom tools using Interface Sniffing (IButlerToolAsyncResolver), how to set up local Ollama models, and how to write custom Providers—please visit our GitHub repository:

👉 ButlerSDK on GitHub

Feedback & Issues: This is currently in Active Preview. Please submit bug reports or feedback directly on our GitHub Issues page or via email at butlersdkfeedback@gmail.com.

License: Apache 2.0

