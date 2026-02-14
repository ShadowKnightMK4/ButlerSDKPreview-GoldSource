***

# ButlerSDK - Preview 1

[![NuGet](https://img.shields.io/badge/nuget-v1.0.0--preview-blue.svg)](https://www.nuget.org/packages/ButlerSDK/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Platform: .NET 8](https://img.shields.io/badge/Platform-.NET%208-purple.svg)](https://dotnet.microsoft.com/)

**Robust, vendor-agnostic AI orchestration for .NET.**

ButlerSDK transforms Large Language Models (LLMs) from unpredictable chatbots into reliable infrastructure components. It provides a unified abstraction layer over **OpenAI**, **Google Gemini**, and **Ollama**, while enforcing tool infrastructure, strict C# Tool typing and opens the door to letting the developer inspect the Streaming LLM to steer it or filter for agentic workflows.

# Setting the Preview expectations
While the infrastructure feels decent to me, there may be some bugs or misspelled things I messaged after spellchecking. Please don't hesitate to email or open a GitHub Issue.

### Example (from the QuickStartOpenAi example)
```CSharp

static bool SimpleHandler(ButlerStreamingChatCompletionUpdate content, IList<ButlerChatMessage> msg)
    {
        // here we just loop thru the content and write it to the console.
        for (int i = 0; i < content.ContentUpdate.Count; i++)
        {
            if (!string.IsNullOrEmpty(content.ContentUpdate[i].Text))
                Console.Write(content.ContentUpdate[i].Text);
        }
        return true;
    }

    static async Task<int> Main(string[] args)
    {

        /* Note the IButlerVaultKeyCollection exists to NOT HARD CODE KEYS. 
         * This is here to show easy usage with the facade 'starter' routine */

        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (apiKey is null)
            throw new InvalidOperationException("OPENAI_API_KEY not set.");




        // create an OpenAi powered butler
        var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt-4o");

        // set a system level prompt for the LLM
        butler.AddSystemMessage("You start each reply with 'what's up' but are helpful otherwise.");
        // give a push
        butler.AddUserMessage("What's the reason for gaming head phones?");

        // fire off the request to OpenAI's servers
        // var EndReason = await butler.StreamResponseAsync(null); // strictly speaking a handler is optional.

        var EndReason = await butler.StreamResponseAsync(SimpleHandler);

        // write the message to front.
        foreach (ButlerChatMessage msg in butler.ChatCollection)
        {
                Console.WriteLine(msg.GetCombinedText());
        }
        return 0;
    }
}
```
### 🛠 Minimum Requirements
- **.NET 8**: [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
- **Windows 10+** (Vault system uses Windows specific routines)
  - *Notice: If using non Windows, there are a few interfaces in the SDK to substitute that aren't just Windows. They don't however offer reading from encrypted data*
- **Visual Studio 2022+** recommended  
- **Local models only** need Ollama installed. You can get it here [Download](https://ollama.com/download).
- **Local models only** Recommended 20GB+ free disk space, GPU (Vega 64 or better, you may need )  
- **Remote models**: OpenAI or Google Gemini account with API key  

### Provider Recommended NuGets
 OpenAI ButlerSDK Provider
- **RECOMMENDED NuGet package is OpenAI (2.8.0) or higher**

Ollama (local) thru OpenAI wrapper 
- ***RECOMMENDED NuGet package is OpenAI (2.8.0) or higher***
- setup tested via 0.25.25 Ollama via local host
- ChatGPT suggested dropping a 'config.toml' in my user profile .Ollama subfolder (directions below).
- That got it to work ok, however you may not necessarily need too.
- powered by OpenAI Provider in ButlerSDK pointed to local host


Gemini
- **RECOMMENDED NuGet is Google_GenerativeAI (3.6.3) or higher**


- ### 💻 Local Models of Interest
- `hf.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF:Q4_K_M` — base target for shipped counter measures (more below) - the ToolPostProcessing class 
- `hf.co/hermes42/Mistral-7B-Instruct-v0.3-imatrix-GGUF:Q4_K_M` — reasonable  
- `hf.co/unsloth/gpt-oss-20b-GGUF:Q4_K_M` — recommended if it fits in GPU memory  
- If you can run GPT-OSS-120B or higher, go for it! Also as a request, I'd like to know how you use Butler with it.


# What is the goal of this preview?

The main goal of this preview is feedback of all kinds for good and ill, and I am open to hearing. This preview is focused on getting feedback from public, particular if something breaks but  warranted positive feedback too.

---

## 🚀 Why ButlerSDK?

Most AI integrations are simple wrappers around an API. ButlerSDK is a **Orchestrator/Conductor**.

1.  **Vendor Agnostic:** Swap between models such `gpt-4o`, `gemini-flash-latest"`, and local like `mistral`, or 'gpt-oss-20b' models without changing a single line of your business logic.
2.  **Self-Healing Agents:** The built-in and opt in **Post-Processing Pipeline** detects tool call hallucinations (e.g., the model *talking* about a tool without *calling* it) and forces a "Remedial" turn to fix the error before the user sees it.
3.  **Capability-Based Security:** Tools are treated as untrusted. The `ToolSurfaceScope` declarative flags requires developers to specify exactly what the tool uses (think surfaces like disk, network, os). The chat session controller has final say in setting allowed permissions on tool add request.
 1. Important. As of this prerelease, this is dev enforced ie if the tool reports surface area, the developer must set the butler class to allow it. That may change later.
4.  **"TrenchCoat" Memory:** A sliding-window context engine that ensures System Prompt and and Tool specific directions never age out of the sliding context window it presents. There's an AudiLog list for review.

---

## 📦 Installation

Download directly [Download](https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource)


*(Note: Currently optimized for Windows due to DPAPI security dependencies. Linux support requires implementing `IButlerVaultKeyCollection`) if you need security. However, the generic InMemoryApiKey class should let you work with the system.*


---

## ⚡ Setup for Butler (after adding it to your project)



### 🐙 Ollama Setup
1. Install Ollama: [https://ollama.com/](https://ollama.com/)  
2. Pull a model (example: GPT-OSS 20B):  
```bash
ollama pull gpt-oss-20b


Ensure Ollama runs in OpenAI compatibility mode:

Create config file (quotes required in Notepad):

C:\Users\yourname\.ollama\config.toml
```

Add the following entry to the file if you have issues to attempt to force OpenAI mode with Ollama.
```bash
[api]
openai_compat = true
```

Then Restart Ollama:

```bash
ollama stop
ollama start
```

Optional: If Ollama is slow on your machine, run this is PowerShell (Windows). It instructs Ollama to use older drivers. You may need to do some debug mode reading (or just paste it into Gemini ai studio to see if your gpu is seen):

```bash
setx OLLAMA_VULKAN "1"
ollama stop
ollama start
```


🔐 Vault Setup

A Vault is encrypted local storage for API keys and secrets.

Windows only support in v1 because the implementation needing a Windows feature.
*Important: The Vault system exists as a way to inject keys to LLM requests and tool calls without hardcoding, ButlerSDK ships with a few out of the box:
1. **InMemoryApiKey** - exists to let someone just plug keys in.
2. **EnvironmentApiKeyMgr** - exists to load keys from environmental variables.
3. **FileApiKeyMgr** - *NOT* recommended, it presents a folder as keys to load from.
1. **TestingApiKeyMgr** - *NOT* recommend. No way to present keys other than 'OPENAI' and 'GEMINI'. Additionally designed to NOT work in RELEASE build.

Run (after compiling):

ApiKeyVaultCreation.exe (no args)  to get the help data.


Follow the on-screen steps.








## ⚡ Development Quick Start (After installation)

ButlerSDK uses a Facade pattern to get you started instantly, while exposing the full architecture for power users.

One Important note and feedback request:
* If a tool requires an API key, you must integrate it via the full Butler SDK (the Facade cannot handle arbitrary keys).
* For example if you use an AzureMaps tool that requests a key, it won't work with a Facade created instance at the currently.
* I am open for adding ability in the Facade to offer adding support to extend the Facade pattern for for IButlerVaultKeyCollection too.


```csharp
using ButlerSDK;
using ButlerToolContract.DataTypes;

// 1. Initialize the Butler (Facade Pattern)
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var butler = ButlerStarter.Instance.CreateOpenAiButler(apiKey, "gpt-4o");

// 2. Add Capability-Safe Tools
butler.AddTool(new ButlerTool_DeviceAPI_GetLocalDateTime()); // (No special permssions) 


 // Requires Network Write permissions, so we adjust the min rights here


butler.ToolSurfaceScope += (ToolSurfaceScope.NetworkWrite | ToolSurfaceScope.NetworkRead);


 // then add the tool
butler.AddTool(new ButlerTool_Network_Ping());   


// 3. Set Directives
butler.AddSystemMessage("You are a helpful engineer who is ready to ping an IP");

butler.AddUserMessage("Please ping 8.8.8.8 and tell me anything you can about it");


// 4. Stream Response with Auto-Tool Execution
// Butler Handlers tool dispatch. All the SDK does is just forward it.
var EndReason =  await butler.StreamResponseAsync((update, history) => 
{
    // Handle text chunks for UI updates
    foreach(var part in update.ContentUpdate)
    {
        Console.Write(part.Text);
    }
    return true;
});
```

---

## 🛡️ Architecture & Features

### 1. The Post-Processing "Counter-Measures"
LLMs can hallucinate or fail to follow strict JSON schemas. ButlerSDK implements an `IButlerPostProcessorHandler` (specifically `ToolPostProcessing`) that acts as a Quality of Service (QOS) layer.

*   **Detection:** It scans the stream for keywords indicating the AI *wanted* to use a tool but failed to generate the call.
*   **Intervention:** It pauses the stream to the user.
*   **Remediation:** It injects a temporary system prompt: *"[ERROR] You discussed using a tool but did not output the JSON. Retry immediately."*
*   **Result:** The user receives the correct answer, unaware that the agent failed and fixed itself in the background.

To use a Post-processing object, pass an instance of it to the request routine.


```csharp
Butler Jeeves = new() /* initialize Jeeves properly */

var FinishReason = Jeeves.StreamResponseAsync(HandlerChatMessageStreamHandler, new ToolPostProcessing(), false, 5, default);
```

### 2. Vendor Abstraction (`IButlerLLMProvider`)
Write your tools once. Run them anywhere. You may need to tweak descriptions.

| Provider | Status | Tool Support | Streaming |
| :--- | :--- | :--- | :--- |
| **OpenAI** | ✅ Production | ✅ Native | ✅ Accumulation Mode |
| **Google Gemini** | ✅ Production | ✅ Native | ✅ One-Shot Mode |
| **Ollama** | ✅ Beta | ✅ Native | ✅ Accumulation Mode |

To implement a custom provider (e.g., Anthropic), implement `IButlerChatClient` and `IButlerLLMProvider`.

Please see the file CreatingACustomProvider.txt for more details in the repo.


### 3. Secure Key Management
ButlerSDK rejects the practice of holding API keys in `string` variables.
*   **Storage:** Keys can be encrypted at rest using `WindowsVault` (DPAPI) on Windows
*   **Memory:** Keys are handled via `SecureString` to prevent memory dump leaks.
*   **Extensibility:** Implement `IButlerVaultKeyCollection` to integrate with Azure KeyVault or AWS Secrets Manager or however you desire to store keys for LLM and tool use.

---

## 🛠️ Defining Tools

Tools in ButlerSDK are strongly typed C# classes and permission-aware. The example below is meant for easy read. You'll probably want to Handle HttpClient() differently in production/final version

```csharp
[ToolSurfaceCapabilities(ToolSurfaceScope.NetworkRead)]
public class GetPublicIPTool : ButlerToolBase
{
    public override string ToolName => "GetPublicIP";
    public override string ToolDescription => "Retrieves the machine's external IP address.";

    public override ButlerChatToolResultMessage ResolveMyTool(string args, string id, ButlerChatToolCallMessage call)
    {
        return ResolveMyToolAsync(FunctionCallArguments, FuncId, Call).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    
    public async Task<ButlerChatToolResultMessage> ResolveMyToolAsync(...)
    {
        var ip = await new HttpClient().GetStringAsync("...");
        return new ButlerChatToolResultMessage(id, ip);
    }

    // JSON Schema definition for the LLM
    public override string GetToolJsonString() => NoArgJson; 
}
```

---

## 🤝 Contributing

This project is currently in **Preview**.
*   **Windows Users:** Fully supported out of the box. "ApiKeyVaultCreation.exe" (after building it) and follow its directions.
*   **Linux/Mac Users:** You should provide a custom implementation of `IButlerVaultKeyCollection` to handle key security; however, you aren't dead on arrival, several Implementations exist for the vault interface to pick from to inject keys too. 


Feedback is welcome:
* 🐛 **Bug Reports:** Please file an Issue on GitHub.
* 🚧 **Friction:** Did the setup confuse you? Let me know.
* 💡 **Success Stories:** Did Butler solve a problem for you?
* You can also email me @ ButlerSDKFeedback@gmail.com.

---

### 📄 License

Apache 2.0 License. See [LICENSE](LICENSE) for details.

Copyright 2025-2026 by Thomas Paul Betterly

---

### ⚠️ IMPORTANT Notes & Limitations

* Version 1 supports text only (no images or audio). Files in particular are dumped directly into the chat log.

* Some data types reference images, but these are placeholders for future releases.

* Local models require sufficient GPU memory; remote models need valid API keys.

* Context Window size matters. If you go from a 1,000,000 size token model to a 5000.00 sized model with too much text, you're probably gonna crash!


### ⚠️ IMPORTANT Notes on this as a Preview

* Stuff can change, including breaking changes.

### ⚠️ Possible Breaking Changes
Please note as with any preview, stuff can change as it stablized.

* TrenchCoatChatCollection is planned to be converted to interface for ButlerBase and let you sub your own solution.
* MockProvider (aka the unit test stub) has gained basic initialize for testing only.
* New INPROGRESS Feature: Butler class (and by extention ButlerBase) can now be provided a generic IButlerChatCollection (and for full tool (ie the temporary messages and post/pre call stuff + prompt injection) IButlerTrenchImplementation)
   - objective of the feature is Assume the simpler one (IButlerChatCollection) and use the fancy one if the passed class supports it.
