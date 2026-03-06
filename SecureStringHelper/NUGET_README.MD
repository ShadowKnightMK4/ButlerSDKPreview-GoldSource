
```markdown
# ButlerSDK.SecureStringHelper (Preview 1)
*A lightweight helper library for working with .NET SecureString.*

`ButlerSDK.SecureStringHelper` provides extension methods that simplify constructing and extracting values from `SecureString` instances. It is designed primarily for **secret lifecycle management**, such as API keys or credentials retrieved from files, environment variables, or other runtime sources.

* **GitHub Repository:**[ShadowKnightMK4/ButlerSDKPreview-GoldSource](https://github.com/ShadowKnightMK4/ButlerSDKPreview-GoldSource)
* **Recommendation:** Consider grabbing `ButlerSDK.Core` and its variants as needed for your project.

---

## ❓ Why This Library Exists

Working with `.NET SecureString` directly can be cumbersome. Construction typically requires adding characters one at a time and explicitly calling `MakeReadOnly()`.

This library provides helpers that:
* Convert **strings or streams into SecureString**
* Automatically **finalize the SecureString as read-only**
* Provide controlled methods to convert the value back to plain text when required by external APIs

This helps reduce repetitive boilerplate while maintaining consistent handling of sensitive values.

---

## 📦 Install

Install from NuGet via the .NET CLI:
```bash
dotnet add package ButlerSDK.SecureStringHelper
```

Or via the Package Manager Console:
```powershell
PM> Install-Package ButlerSDK.SecureStringHelper
```

---

## 🚀 Basic Usage

### 1. Create from a plain string
```csharp
using SecureStringHelper;
using System.Security;

var secret = new SecureString();
secret.AssignStringThenReadOnly("MySuperSecretPassword");
```

### 2. Create from a stream
Useful when secrets are read from files, memory streams, or embedded resources.
```csharp
using System.IO;

var secureFromStream = new SecureString();
secureFromStream.AssignStringThenReadOnly(myMemoryStream);
```

### 3. Convert back to a string
Some SDKs require plain text credentials. When necessary, a `SecureString` can be safely converted back to a string, automatically handling the memory pointer cleanup.


```csharp
string plainText = secret.DecryptString();
```

---

## 🔄 Typical Use Case

This library is meant to be used in conjunction with **runtime secret providers**, for example:
* Environment variables
* API key files
* In-memory configuration
* Secret vault providers

**Example flow:**
```text[Environment Variable] 
        ↓
[SecureStringHelper]
        ↓
[SecureString (Protected in Memory if the .NET runtime supports it)]
        ↓
[API SDK requiring string]
```
This approach helps keep secrets contained in protected memory structures for most of their lifecycle.

---

## ⚠️ Security Disclaimer

ButlerSDK uses these extensions primarily for their **`IDisposable`** benefits and memory-clearing properties, and **NOT** as a bulletproof cryptographic solution.

Important considerations:
* If a `SecureString` is created from an existing `string`, that plain string already exists in managed memory.
* Some modern .NET platforms do not provide strong memory protection guarantees for `SecureString`.
* Many external APIs ultimately require credentials as plain strings.

For these reasons, `SecureStringHelper` should be viewed as a **memory lifecycle and cleanup utility**, not a complete secret-management solution. Best practice, when possible, is to construct `SecureString` values **character-by-character from user input** rather than converting existing strings.

---

## 📄 License
**Apache 2.0 License**. See the [LICENSE](LICENSE) file for details.

Copyright © 2025-2026 by Thomas Paul Betterly
```