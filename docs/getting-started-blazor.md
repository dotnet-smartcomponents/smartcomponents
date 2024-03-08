# Getting started with Smart Components in Blazor

1. **Create a new .NET 8 Blazor project or use an existing one.**

   * Command line: Run `dotnet new blazor`
   * Visual Studio: Select *File*->*New*->*Project...* then choose *Blazor Web App*

   **Note**: Smart Components work equally in any render mode (e.g., Static SSR, Server, or WebAssembly) but you do need to have an ASP.NET Core server, so you cannot use a Blazor WebAssembly Standalone App hosted on a static file server. This is purely because you need a server to hold your API keys securely.

1. **Install packages**

   * In your **server** project, install the NuGet package `SmartComponents.AspNetCore`.

     * Command line: `dotnet add package SmartComponents.AspNetCore`
     * Visual Studio: Right-click your project name, choose *Manage NuGet packages...*, and then search for and install `SmartComponents.AspNetCore`.

   * If you also have a **WebAssembly** project, install the NuGet package `SmartComponents.AspNetCore.Components` into it. This is not required if you only have a server project.

1. **Register SmartComponents in your application**

   In your server's `Program.cs`, under the comment `// Add services to the container`, add:

   ```cs
   builder.Services.AddSmartComponents();
   ```

1. **Configure the OpenAI backend** (if needed)

   If you will be using either `SmartPaste` or `SmartTextArea`, you need to provide access to a language model backend. See: [Configure the OpenAI backend](configure-openai-backend.md).
   
   If you will only use `SmartComboBox`, you don't need any language model backend and can skip this step.

1. **Add components to your pages**

   You can now add the following inside your Blazor pages/components:

   * [SmartPaste](smart-paste.md)
   * SmartTextArea
   * SmartComboBox
